using Core.Config.Constants.LoggingConstants;
using Core.Constants.ProblemDetailsTitles;
using Core.Domain.AINurturingAggregate;
using Core.Domain.NotificationAggregate;
using Core.DTOs.ProcessingDTOs;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Exceptions;
using Web.ApiModels.APIResponses.ActionPlans;
using Web.ApiModels.APIResponses.AINurturings;
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

        public async Task<List<AINurturingDTO>> GetMyAINurturingsAsync(Guid brokerId)
        {
            var broker = _appDbContext.Brokers.FirstOrDefault(x => x.Id == brokerId);
            if (broker == null)
            {
                _logger.LogError($"No broker with Id {brokerId}");
                throw new CustomBadRequestException($"No broker with Id {brokerId}", ProblemDetailsTitles.NotFound);
            }

            var aiNurturings = _appDbContext.AINurturings.Where(a => a.BrokerId == brokerId);
            return await ConvertAINurturingsAsync(aiNurturings);
        }

        public async Task<List<AINurturingDTO>> GetLeadAINurturingsAsync(int leadId)
        {
            var lead = _appDbContext.Leads.FirstOrDefault(x => x.Id == leadId);
            if (lead == null)
            {
                _logger.LogError($"No lead with Id {leadId}");
                throw new CustomBadRequestException($"No lead with Id {leadId}", ProblemDetailsTitles.NotFound);
            }

            var aiNurturings = _appDbContext.AINurturings.Where(a => a.LeadId == leadId);
            return await ConvertAINurturingsAsync(aiNurturings);
        }

        private async Task<List<AINurturingDTO>> ConvertAINurturingsAsync(IQueryable<AINurturing> aiNurturings)
        {
            var convertedNurturings = aiNurturings.Select(x => new AINurturingDTO
            {
                Id = x.Id,
                IsActive = x.IsActive,
                TimeCreated = x.TimeCreated,
                Status = x.Status,
                AnalysisStatus = x.AnalysisStatus,
                LastFollowupDate = x.LastFollowupDate,
                FollowUpCount = x.FollowUpCount,
                GmailThreadId = x.ThreadId,
                InitialMessageSent = x.InitialMessageSent,
                LastProcessedMessageTime = x.LastProcessedMessageTime,
                QuestionsCount = x.QuestionsCount,
                Lead = new LeadNameIdDTO()
                {
                    firstName = x.lead.LeadFirstName,
                    lastName = x.lead.LeadLastName,
                    LeadId = x.LeadId
                }
            });

            return await convertedNurturings.ToListAsync();
        }

        public async Task StopAINurturing(int leadId)
        {
            var aiNurturing = _appDbContext.AINurturings.FirstOrDefault(x => x.LeadId == leadId && x.IsActive);
            if (aiNurturing == null)
            {
                _logger.LogError($"No nurturing for lead with Id {leadId}");
                throw new CustomBadRequestException($"No nurturing for lead with Id {aiNurturing}", ProblemDetailsTitles.NotFound);
            }

            if (!aiNurturing.IsActive)
            {
                _logger.LogError($"The nurturing for lead with Id {leadId} has already been stopped");
                throw new CustomBadRequestException($"The nurturing for lead with Id {leadId} has already been stopped", ProblemDetailsTitles.AlreadyPerformed);
            }

            aiNurturing.Status = AINurturingStatus.Cancelled;
            aiNurturing.IsActive = false;

            _appDbContext.Entry(aiNurturing).State = EntityState.Modified;
            await _appDbContext.SaveChangesAsync();
        }

        public async Task<AINurturingStartDTO> StartAINurturing(Guid brokerId, StartNurturingDTO dto)
        {
            var broker = _appDbContext.Brokers.Include(x => x.ConnectedEmails).FirstOrDefault(x => x.Id == brokerId);
            if (broker == null)
            {
                _logger.LogError($"No broker with Id {brokerId}");
                throw new CustomBadRequestException($"No broker with Id {brokerId}", ProblemDetailsTitles.NotFound);
            }

            var connectedEmail = broker.ConnectedEmails?.FirstOrDefault(e => e.isMailbox);
            if (connectedEmail == null)
            {
                _logger.LogError($"No connectedEmail for broker with Id {brokerId}");
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
    }
}
