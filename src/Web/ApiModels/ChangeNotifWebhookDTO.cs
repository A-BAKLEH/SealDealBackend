namespace Web.ApiModels;

public class ChangeNotifWebhookDTO
{
    public List<ChangeNotifWebhookDTOValue> value { get; set; }
}

public class ChangeNotifWebhookDTOValue
{
    public string subscriptionId { get; set; }
    public string changeType { get; set; }
    public string clientState { get; set; }
    public string tenantId { get; set; }
}
/*
 * {"value":[
	{"subscriptionId":"0f02734d-2f84-4c08-b185-692cd5a693a3",
	"subscriptionExpirationDateTime":"2023-06-03T20:25:05.0797291+00:00",
	"changeType":"created",
	"resource":"Users/dc6a587c-e51d-4e4b-9b95-d35c3e289b53/Messages/AAMkADJmNWUwNzZlLWMxNWQtNGNkMy1iNmY4LWJiOTBkYjk5YjVlYgBGAAAAAAC6hwbZVh5-T6nLJ1FvBfvIBwBZbQVR_TVZTLodSM-vZvBeAAAAAAEMAABZbQVR_TVZTLodSM-vZvBeAAB3MSDJAAA=",
	"resourceData":{"@odata.type":"#Microsoft.Graph.Message","@odata.id":"Users/dc6a587c-e51d-4e4b-9b95-d35c3e289b53/Messages/AAMkADJmNWUwNzZlLWMxNWQtNGNkMy1iNmY4LWJiOTBkYjk5YjVlYgBGAAAAAAC6hwbZVh5-T6nLJ1FvBfvIBwBZbQVR_TVZTLodSM-vZvBeAAAAAAEMAABZbQVR_TVZTLodSM-vZvBeAAB3MSDJAAA=",
	"@odata.etag":"W/\"CQAAABYAAABZbQVR+TVZTLodSM/vZvBeAAB3AQa8\"",
	"id":"AAMkADJmNWUwNzZlLWMxNWQtNGNkMy1iNmY4LWJiOTBkYjk5YjVlYgBGAAAAAAC6hwbZVh5-T6nLJ1FvBfvIBwBZbQVR_TVZTLodSM-vZvBeAAAAAAEMAABZbQVR_TVZTLodSM-vZvBeAAB3MSDJAAA="},
	"clientState":"secretToMakeSureNotifsAreLegit",
	"tenantId":"d0a40b73-985f-48ee-b349-93b8a06c8384"}
	]
}
 * 
 * 
 */