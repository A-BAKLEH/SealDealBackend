using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.AINurturingAggregate;
using Core.Domain.LeadAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Pipelines.Sockets.Unofficial.Arenas;
using SharedKernel.Exceptions;
using Web.ApiModels.RequestDTOs.AINurturing;
using Web.Constants;
using Web.Processing.ActionPlans;

namespace Web.ControllerServices.QuickServices
{
    public class AINurturingQService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<ActionPQService> _logger;
        public AINurturingQService(ILogger<ActionPQService> logger, AppDbContext appDbContext)
        {
            _logger = logger;
            _appDbContext = appDbContext;
        }

        public async Task<AINurturingStartDTO> StartAINurturing(Guid brokerId, StartNurturingDTO dto)
        {
            var broker = _appDbContext.Brokers.Include(x => x.ConnectedEmails).FirstOrDefault(x => x.Id == brokerId);
            if (broker == null)
            {
                _logger.LogError("{tag} no broker with Id {brokerId}", "ExecuteSendEmail", brokerId);
                throw new CustomBadRequestException($"No broker with Id {brokerId}", ProblemDetailsTitles.NotFound);
            }

            var connectedEmail = broker.ConnectedEmails?.FirstOrDefault(e => e.isMailbox);
            if (connectedEmail == null)
            {
                _logger.LogError("{tag} no connectedEmail for broker with Id {brokerId}", "ExecuteSendEmail", brokerId);
                throw new CustomBadRequestException($"No connectedEmail for broker with Id {brokerId}", ProblemDetailsTitles.NoSuitableEmail);
            }
            else
            {
                if (connectedEmail.isMSFT && !connectedEmail.hasAdminConsent)
                {

                    throw new CustomBadRequestException($"No suitable email for broker with Id {brokerId}", ProblemDetailsTitles.NoSuitableEmail);
                }
            }

            var leads = await _appDbContext.Leads.Include(x => x.AINurturings)
                .Where(x => x.BrokerId == brokerId && dto.LeadIds.Contains(x.Id))
                .ToListAsync();

            List<int> failedIDs = new();
            List<int> AlreadyRunningIDs = new();

            foreach (var lead in leads)
            {
                if (lead.AINurturings != null && lead.AINurturings.Any(x => x.IsActive && x.BrokerId == brokerId))
                {
                    AlreadyRunningIDs.Add(lead.Id);
                }
            }

            if (AlreadyRunningIDs.Count == leads.Count)
            {
                throw new CustomBadRequestException("The leads already have active AI nurturings", ProblemDetailsTitles.AINurturingAlreadyEnabled);
            }

            leads.RemoveAll(l => AlreadyRunningIDs.Contains(l.Id));
            var timeNow = DateTime.UtcNow;

            foreach (var lead in leads)
            {
                try
                {
                    var aiNurturing = new AINurturing()
                    {
                        BrokerId = brokerId,
                        IsActive = true,
                        FollowUpCount = 0,
                        LeadId = lead.Id,
                        QuestionsCount = 0,
                        Status = AINurturingStatus.Running,
                        TimeCreated = timeNow,
                        ThreadId = "",
                    };

                    var APStartedEvent = new AppEvent
                    {
                        BrokerId = brokerId,
                        EventTimeStamp = timeNow,
                        EventType = EventType.AINurturingStarted,
                        IsActionPlanResult = true,
                        ReadByBroker = true,
                        ProcessingStatus = ProcessingStatus.NoNeed,
                    };

                    APStartedEvent.Props[NotificationJSONKeys.APTriggerType] = NotificationJSONKeys.TriggeredManually;
                    lead.AppEvents = new() { APStartedEvent };
                    _appDbContext.AINurturings.Add(aiNurturing);
                    await _appDbContext.SaveChangesAsync();

                    BackgroundJob.Schedule<NurturingProcessor>((x) => x.SendInitialMessage(aiNurturing.Id), TimeSpan.FromSeconds(0));

                }
                catch (Exception ex)
                {
                    _logger.LogError($"an Error occured starting AI Nurturing for Lead {lead.Id} with error {ex.Message}", TagConstants.AiNurturingStarting, lead.Id, ex.Message + " :" + ex.StackTrace);
                    failedIDs.Add(lead.Id);
                }
            }

            return new AINurturingStartDTO { AlreadyRunningIDs = AlreadyRunningIDs, errorIDs = failedIDs };
        }

        public async Task Test()
        {
            try
            {
                //BackgroundJob.Schedule<NurturingProcessor>((x) => x.ProcessEmailReply(new Processing.Nurturing.NurturingEmailEvent()
                //{
                //    DecodedEmail = new Processing.EmailAutomation.EmailProcessor.GmailMessageDecoded()
                //    {
                //        textBody = "test"
                //    },
                //    NurturingId = 4
                //}), TimeSpan.FromSeconds(0));

                var nurturing = _appDbContext.AINurturings.Include(x => x.broker).ThenInclude(x => x.Agency).Include(x => x.lead).FirstOrDefault(x => x.Id == 4);
                nurturing.QuestionsCount = 23;
                _appDbContext.Entry(nurturing).State = EntityState.Modified;
                await _appDbContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {

            }
        }
    }
}
