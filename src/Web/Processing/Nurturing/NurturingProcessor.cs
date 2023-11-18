using Hangfire;
using Hangfire.Server;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;
using System.Diagnostics;
using Web.HTTPClients;
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
                .Where(x => x.IsActive).ToListAsync();


            foreach (var aiNurturing in activeNurturings)
            {
                try
                {
                    if (aiNurturing.LastProcessedMessageTime == null || DateTime.UtcNow - aiNurturing.LastProcessedMessageTime > /*TimeSpan.FromDays(settingsFollowUpDelayDays)*/ TimeSpan.FromMinutes(settingsFollowUpDelayDays))
                    {
                        if (DateTime.UtcNow - aiNurturing.LastFollowupDate > /*TimeSpan.FromDays(settingsFollowUpDelayDays)*/ TimeSpan.FromMinutes(settingsFollowUpDelayDays))
                        {
                            if (aiNurturing.FollowUpCount >= settingsFollowUpMax)
                            {
                                aiNurturing.IsActive = false;
                                aiNurturing.Status = AINurturingStatus.StoppedNoResponse;
                                _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

                                await _appDbContext.SaveChangesAsync();
                            }
                            else
                            {
                                var threadHistory = await _actionExecuter.FetchThreadHistory(aiNurturing.BrokerId, aiNurturing.ThreadId);
                                OpenAIResponse response = await _openAIService.ProccessAINurturing(NurturingProcessingType.FollowUp, aiNurturing.broker, aiNurturing.lead, threadHistory);

                                if (response.Success)
                                {
                                    var result = await _actionExecuter.ExecuteSendNurturingEmail(aiNurturing.BrokerId, aiNurturing.LeadId, aiNurturing.Id, response.TextReply, null, aiNurturing.ThreadId, "Following Up on Your Property Search Inquiry");

                                    if (result.Success)
                                    {
                                        aiNurturing.FollowUpCount++;
                                        aiNurturing.LastFollowupDate = DateTime.UtcNow;

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
                    _logger.LogError($"Couldn't process follow ups for the following nurturing - {aiNurturing.Id}");
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
            throw new InvalidOperationException($"Error generating the initial email for the lead; Nurturing - {aiNurturing.Id}");
        }

        var emailResult = await _actionExecuter.ExecuteSendNurturingEmail(aiNurturing.BrokerId, aiNurturing.LeadId, aiNurturing.Id, response.TextReply, null, null, "Inquiry About Your Property Search");

        if (!emailResult.Success)
        {
            _logger.LogError($"Couldn't send the initial message for a nurturing with id - {aiNurturing.Id}");
            throw new InvalidOperationException($"Error sending the initial email to the lead; Nurturing - {aiNurturing.Id}");
        }

        aiNurturing.InitialMessageSent = true;
        aiNurturing.ThreadId = emailResult.ThreadId;
        aiNurturing.LastFollowupDate = DateTime.UtcNow;

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

        if (!aiNurturing.IsActive)
        {
            _logger.LogInformation($"The nurturing with id - {emailEvent.NurturingId} is not active");

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
                aiNurturing.IsActive = false;
                aiNurturing.Status = AINurturingStatus.Done;
                aiNurturing.AnalysisStatus = response.LeadAnalysis.FinalStatus;
                _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

                await _appDbContext.SaveChangesAsync();
                //send sms to broker
            }
            else
            {
                throw new InvalidOperationException($"Error generating an email for the lead; Nurturing - {aiNurturing.Id}");
            }
        }
        else
        {
            var threadHistory = await _actionExecuter.FetchThreadHistory(aiNurturing.BrokerId, aiNurturing.ThreadId);
            OpenAIResponse response = await _openAIService.ProccessAINurturing(NurturingProcessingType.AskingQuestions, aiNurturing.broker, aiNurturing.lead, threadHistory);

            if (response.Success)
            {
                var result = await _actionExecuter.ExecuteSendNurturingEmail(aiNurturing.BrokerId, aiNurturing.LeadId, aiNurturing.Id, response.TextReply, emailEvent.DecodedEmail.message.Id);

                if (result.Success)
                {
                    aiNurturing.QuestionsCount++;
                    aiNurturing.LastProcessedMessageTime = emailEvent.DecodedEmail.timeReceivedUTC;
                    aiNurturing.LastFollowupDate = DateTime.UtcNow;

                    _appDbContext.Entry(aiNurturing).State = EntityState.Modified;

                    await _appDbContext.SaveChangesAsync();
                }
                else
                {
                    throw new InvalidOperationException($"Error sending an email for the lead; Nurturing - {aiNurturing.Id}");
                }
            }
        }
    }
}
