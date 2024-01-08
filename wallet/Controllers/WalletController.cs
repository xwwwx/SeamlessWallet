using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using wallet.Models.Controllers;
using wallet.Models.Controllers.Wallet.Request;
using wallet.Models.Controllers.Wallet.Response;

namespace wallet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController(WalletService walletService) : ControllerBase
    {
        [HttpGet]
        public async Task<GetWalletResponse> GetWalletAsync([FromQuery] Guid walletId)
        {
            var wallet = await walletService.GetWalletAsync(walletId);
            if (wallet == null)
            {
                return new()
                {
                    Code = ResponseCode.WalletNotExists,
                    Message = "Wallet Not Exists!"
                };
            }

            return new()
            {
                Data = new(wallet.WalletId)
                {
                    Amount = wallet.Amount
                }
            };
        }

        [HttpPost]
        public async Task<PostWalletResponse> PostWalletAsync([FromBody] PostWalletRequest request)
        {
            var wallet = await walletService.PostWalletAsync(request.WalletId);
            return new()
            {
                Data = new(wallet.WalletId)
                {
                    Amount = wallet.Amount
                }
            };
        }

        [HttpPut]
        public async Task<WalletTransferResponse> WalletTransferAsync([FromBody] WalletTransferRequest request)
        {
            var transferRecord =
                await walletService.TransferAsync(request.TransferRecordId, request.WalletId, request.Amount);
            return new()
            {
                Data = new()
                {
                    TransferRecordId = transferRecord.TransferRecordId,
                    WalletId = transferRecord.WalletId,
                    Amount = transferRecord.Amount,
                    BeforeAmount = transferRecord.BeforeAmount,
                    AfterAmount = transferRecord.AfterAmount,
                    CreatedTime = transferRecord.CreatedTime,
                }
            };
        }
    }
}
