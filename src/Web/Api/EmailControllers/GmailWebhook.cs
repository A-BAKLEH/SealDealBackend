using Infrastructure.Data;
using MediatR;
using Web.ControllerServices;

namespace Web.Api.EmailControllers;

public class GmailWebhook : BaseApiController
{
    private readonly ILogger<GmailWebhook> _logger;
    //private readonly IDistributedCache _distributedCache;
    private AppDbContext _appDbContext;

    public const string EmailFetchTaskKey = "EmailFetchTask";
    public GmailWebhook(AuthorizationService authorizeService, IMediator mediator, AppDbContext appDbContext,
        ILogger<GmailWebhook> logger
        //IDistributedCache distributedCache
        ) : base(authorizeService, mediator)
    {
        _logger = logger;
        //_distributedCache = distributedCache;
        _appDbContext = appDbContext;
    }

    //[HttpPost("Gmail-webhook")]
    //public async Task<IActionResult> GmailWebhookExecute()
    //{
    //  //would be sent by gmail push notification
    //  //this is a broker's connected email
    //  var email = "someone@gmail.com";
    //  //TODO change error message
    //  /*_ = _mediator.Send(new EmailWebhookRequest { isGmail = true, email = email }).
    //    ContinueWith(t => _logger.LogError("error",t.Exception),
    //      TaskContinuationOptions.OnlyOnFaulted);*/
    //  return Ok();
    // }
    //-> use Hisotry.List(token) API to get a list of {emailId, type of notif (added, deleted, whatever)}
}
