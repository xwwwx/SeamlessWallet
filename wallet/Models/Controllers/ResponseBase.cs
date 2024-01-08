namespace wallet.Models.Controllers
{
    public class ResponseBase
    {
        public ResponseCode Code { get; set; } = ResponseCode.Success;

        public string Message { get; set; } = "Success";
    }
}
