using Core.Config.Constants.LoggingConstants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.ControllerServices.StaticMethods;

namespace Web.Api.NotificationsController;

[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly NotificationService _notificationService;
    public NotificationsController(AuthorizationService authorizeService, NotificationService notificationService, IMediator mediator, ILogger<NotificationsController> logger) : base(authorizeService, mediator)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// all notifs for signin
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Notifs/All")]
    public async Task<IActionResult> GetALLDashboardNotifs()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} Inactive User tried to GetNotifs ", TagConstants.Unauthorized);
            return Forbid();
        }

        var res = await _notificationService.GetAllDashboardNotifs(brokerTuple.Item1.Id);

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);

        foreach (var leadDTO in res.LeadRelatedNotifs)
        {
            if (leadDTO.LastTimeYouViewedLead != null)
                leadDTO.LastTimeYouViewedLead = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)leadDTO.LastTimeYouViewedLead);
            if (leadDTO.MostRecentEventOrEmailTime != null)
                leadDTO.MostRecentEventOrEmailTime = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)leadDTO.MostRecentEventOrEmailTime);
            if (leadDTO.AppEvents != null)
                foreach (var appEvent in leadDTO.AppEvents)
                {
                    appEvent.EventTimeStamp = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, appEvent.EventTimeStamp);
                }
            if (leadDTO.EmailEvents != null)
                foreach (var emailEvent in leadDTO.EmailEvents)
                {
                    emailEvent.Received = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, emailEvent.Received);
                }
            if (leadDTO.PriorityNotifs != null)
                foreach (var p in leadDTO.PriorityNotifs)
                {
                    p.EventTimeStamp = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, p.EventTimeStamp);
                }
        }
        if (res.OtherNotifs != null)
        {
            foreach (var e in res.OtherNotifs)
            {
                e.EventTimeStamp = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, e.EventTimeStamp);
            }
        }
        return Ok(res);
    }

    /// <summary>
    /// all notifs for signin
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Notifs/NormalTable")]
    public async Task<IActionResult> UpdateDashboardNormalTable()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} Inactive User tried to GetNotifs ", TagConstants.Unauthorized);
            return Forbid();
        }

        var res = await _notificationService.UpdateNormalTable(brokerTuple.Item1.Id);
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        foreach (var leadDTO in res.LeadRelatedNotifs)
        {
            if (leadDTO.LastTimeYouViewedLead != null)
                leadDTO.LastTimeYouViewedLead = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)leadDTO.LastTimeYouViewedLead);
            if (leadDTO.MostRecentEventOrEmailTime != null)
                leadDTO.MostRecentEventOrEmailTime = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)leadDTO.MostRecentEventOrEmailTime);
            if (leadDTO.AppEvents != null)
                foreach (var appEvent in leadDTO.AppEvents)
                {
                    appEvent.EventTimeStamp = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, appEvent.EventTimeStamp);
                }
            if (leadDTO.EmailEvents != null)
                foreach (var emailEvent in leadDTO.EmailEvents)
                {
                    emailEvent.Received = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, emailEvent.Received);
                }
        }
        return Ok(res);
    }

    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Notifs/Priority")]
    public async Task<IActionResult> UpdateDashboardPriorityTable()
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} Inactive User tried to GetNotifs ", TagConstants.Unauthorized);
            return Forbid();
        }
        var res = await _notificationService.UpdatePriorityTable(id);
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        foreach (var leadDTO in res.LeadRelatedNotifs)
        {
            if (leadDTO.LastTimeYouViewedLead != null)
                leadDTO.LastTimeYouViewedLead = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)leadDTO.LastTimeYouViewedLead);
            if (leadDTO.MostRecentEventOrEmailTime != null)
                leadDTO.MostRecentEventOrEmailTime = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)leadDTO.MostRecentEventOrEmailTime);
            if (leadDTO.PriorityNotifs != null)
                foreach (var p in leadDTO.PriorityNotifs)
                {
                    p.EventTimeStamp = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, p.EventTimeStamp);
                }
        }
        return Ok(res);
    }

    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("Dashboard/Notifs/{LeadId}/{Normal}/{Priority}")]
    public async Task<IActionResult> GetDashboardNotifsPerLead(int LeadId, bool Normal, bool Priority)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} Inactive User tried to GetNotifs ", TagConstants.Unauthorized);
            return Forbid();
        }
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(brokerTuple.Item1.TimeZoneId);
        var res = await _notificationService.GetPerLeadNewNotifs(id, LeadId, Normal, Priority);

        if (res.LastTimeYouViewedLead != null)
            res.LastTimeYouViewedLead = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)res.LastTimeYouViewedLead);
        if (res.MostRecentEventOrEmailTime != null)
            res.MostRecentEventOrEmailTime = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, (DateTime)res.MostRecentEventOrEmailTime);
        if (res.AppEvents != null)
            foreach (var appEvent in res.AppEvents)
            {
                appEvent.EventTimeStamp = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, appEvent.EventTimeStamp);
            }
        if (res.EmailEvents != null)
            foreach (var emailEvent in res.EmailEvents)
            {
                emailEvent.Received = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, emailEvent.Received);
            }
        if (res.PriorityNotifs != null)
            foreach (var p in res.PriorityNotifs)
            {
                p.EventTimeStamp = MyTimeZoneConverter.ConvertFromUTC(timeZoneInfo, p.EventTimeStamp);
            }
        return Ok();
    }


    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    [HttpGet("Lead/MarkRead/{LeadId}")]
    public async Task<IActionResult> MarkLeadNotifsRead(int LeadId)
    {
        var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
        var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
        if (!brokerTuple.Item2)
        {
            _logger.LogCritical("{tag} Inactive User tried to GetNotifs ", TagConstants.Unauthorized);
            return Forbid();
        }

        await _notificationService.MarkLeadNotifsRead(LeadId, id);
        return Ok();
    }
    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    //[HttpGet("Dashboard/Notifs/Other")]
    //public async Task<IActionResult> GetOtherNotifs()
    //{
    //    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    //    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    //    if (!brokerTuple.Item2)
    //    {
    //        _logger.LogWarning("[{Tag}] Inactive User tried to GetNotifs ", TagConstants.Unauthorized);
    //        return Forbid();
    //    }
    //    return Ok();
    //}
    /// <summary>
    /// Notifs for dashboard table
    /// </summary>
    /// <returns></returns>
    //[HttpGet("BrokerNotifs")]
    //public async Task<IActionResult> GetBrokerNotifs()
    //{
    //    var id = Guid.Parse(User.Claims.ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
    //    var brokerTuple = await this._authorizeService.AuthorizeUser(id, true);
    //    if (!brokerTuple.Item2)
    //    {
    //        _logger.LogWarning("[{Tag}] Inactive User tried to GetNotifs ", TagConstants.Unauthorized);
    //        return Forbid();
    //    }
    //    return Ok();
    //}

}
