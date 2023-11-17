using Core.Config.Constants.LoggingConstants;
using Core.Domain.ActionPlanAggregate;
using Core.Domain.AINurturingAggregate;
using Core.Domain.BrokerAggregate.Templates;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Hangfire.Server;
using Infrastructure.Data;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Diagnostics;
using Web.Constants;
using Web.HTTPClients;
using Web.Processing.EmailAutomation;
using Web.Processing.Nurturing;
using Web.RealTimeNotifs;

namespace Web.Processing.ActionPlans;

public class NurturingProcessor
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<NurturingProcessor> _logger;
    public readonly ActionExecuter _actionExecuter;
    private readonly RealTimeNotifSender _realTimeNotif;
    private readonly OpenAIGPT35Service _openAIService;

    public static int MaxAmountOfEmails = 3;

    public NurturingProcessor(AppDbContext appDbContext, RealTimeNotifSender realTimeNotifSender, OpenAIGPT35Service openAIGPT35Service, ActionExecuter actionExecuter, ILogger<NurturingProcessor> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _actionExecuter = actionExecuter;
        _realTimeNotif = realTimeNotifSender;
        _openAIService = openAIGPT35Service;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task DoActionAsync(PerformContext performContext)
    {
        var settingsFollowUpMax = 2;
        var settingsFollowUpDelayDays = 7;

        Debug.WriteLine($"Nurturing triggered {DateTime.UtcNow}");
        using (LogContext.PushProperty("hangfireJobId", performContext.BackgroundJob.Id))
        {
            var activeNurturings = await _appDbContext.AINurturings
                .Include(x => x.broker).ThenInclude(x => x.Agency).Include(x =>  x.lead)
                .Where(x => x.Status == AINurturingStatus.Running).ToListAsync();


            foreach (var aiNurturing in activeNurturings)
            {
                try
                {
                    if (DateTime.UtcNow - aiNurturing.LastReplyDate > TimeSpan.FromMinutes(settingsFollowUpDelayDays))
                    {
                        if (aiNurturing.LastFollowupDate == null || DateTime.UtcNow - aiNurturing.LastFollowupDate > TimeSpan.FromMinutes(settingsFollowUpDelayDays))
                        {
                            if (aiNurturing.FollowUpCount >= settingsFollowUpMax)
                            {
                                aiNurturing.Status = AINurturingStatus.StoppedNoResponse;
                                _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

                                await _appDbContext.SaveChangesAsync();
                            }
                            else
                            {
                                OpenAIResponse response = await _openAIService.ProccessAINurturing(NurturingProcessingType.FollowUp, aiNurturing.broker, aiNurturing.lead);

                                if (response.Success)
                                {
                                    var result = await _actionExecuter.ExecuteSendNurturingEmail(aiNurturing.BrokerId, aiNurturing.LeadId, aiNurturing.Id, response.TextReply, aiNurturing.ThreadId);

                                    if (result.Success)
                                    {
                                        aiNurturing.FollowUpCount++;
                                        _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

                                        await _appDbContext.SaveChangesAsync();
                                    }
                                }
                                else
                                {
                                    _logger.LogError($"Couldn't generate an AI message for a nurturing followup. Id - {aiNurturing.Id}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendInitialMessage(int nurturingId)
    {
        var aiNurturing = _appDbContext.AINurturings.Include(x => x.broker).ThenInclude(x => x.Agency).Include(x => x.lead).FirstOrDefault(x => x.Id == nurturingId);

        if (aiNurturing == null)
        {
            _logger.LogInformation($"The nurturing with id - {nurturingId} is missing");

            return;
        }

        if (aiNurturing.InitialMessageSent)
        {
            _logger.LogError($"an initial message for {nurturingId} nurturing has already been sent");
            return;
        }

        OpenAIResponse response = await _openAIService.ProccessAINurturing(NurturingProcessingType.SendingInitialMessage, aiNurturing.broker, aiNurturing.lead);
        if (!response.Success)
        {
            _logger.LogError($"Couldn't generate an AI message for a nurturing initial message. Id - {aiNurturing.Id}");
            return;
        }

        var emailResult = await _actionExecuter.ExecuteSendNurturingEmail(aiNurturing.BrokerId, aiNurturing.LeadId, aiNurturing.Id, response.TextReply);

        if (!emailResult.Success)
        {
            _logger.LogError($"Couldn't send the initial message for a nurturing with id - {aiNurturing.Id}");
            return;
        }

        aiNurturing.ThreadId = emailResult.ThreadId;
        aiNurturing.InitialMessageSent = true;
        _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

        await _appDbContext.SaveChangesAsync();
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessEmailReply(NurturingEmailEvent emailEvent)
    {
        var aiNurturing = _appDbContext.AINurturings.Include(x => x.broker).ThenInclude(x => x.Agency).Include(x => x.lead).FirstOrDefault(x => x.Id == emailEvent.NurturingId);

        if (aiNurturing == null)
        {
            _logger.LogInformation($"The nurturing with id - {emailEvent.NurturingId} is missing");

            return;
        }

        if (aiNurturing.LastProcessedMessageTime >= emailEvent.DecodedEmail.timeReceivedUTC)
        {
            return;
        }

        if (aiNurturing.QuestionsCount >= MaxAmountOfEmails)
        {
            var threadHistory = await _actionExecuter.FetchThreadHistory(aiNurturing.BrokerId, aiNurturing.ThreadId);
            OpenAIResponse response = await _openAIService.ProccessAINurturing(NurturingProcessingType.AnalysingLead, aiNurturing.broker, aiNurturing.lead, threadHistory);

            if (response.Success)
            {
                try
                {
                    var deserializedAnalysis = JsonConvert.DeserializeObject<NurturingResult>(response.TextReply);

                    aiNurturing.AnalysisStatus = deserializedAnalysis.FinalStatus;
                    _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

                    await _appDbContext.SaveChangesAsync();

                    //send sms to broker
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error processing analysis for the following nurturing - {aiNurturing.Id}; {ex.Message}");
                }
            }
        }
        else
        {
            var threadHistory = await _actionExecuter.FetchThreadHistory(aiNurturing.BrokerId, aiNurturing.ThreadId);
            OpenAIResponse response = await _openAIService.ProccessAINurturing(NurturingProcessingType.AskingQuestions, aiNurturing.broker, aiNurturing.lead, threadHistory);

            if (response.Success)
            {

                //await _actionExecuter.ReplyToEmailById(aiNurturing.BrokerId, aiNurturing.LeadId, emailEvent.DecodedEmail.message.Id, response.TextReply);
                var result = await _actionExecuter.ExecuteSendNurturingEmail(aiNurturing.BrokerId, aiNurturing.LeadId, aiNurturing.Id, response.TextReply, emailEvent.DecodedEmail.message.Id);

                //if (result.Success)
                //{
                //    try
                //    {
                //        aiNurturing.QuestionsCount++;
                //        _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

                //        await _appDbContext.SaveChangesAsync();
                //    }
                //    catch (Exception ex)
                //    {

                //    }
                //}
            }
        }
    }
}
