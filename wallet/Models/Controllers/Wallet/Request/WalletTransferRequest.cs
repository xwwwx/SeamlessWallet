using System.ComponentModel.DataAnnotations;

namespace wallet.Models.Controllers.Wallet.Request
{
    public class WalletTransferRequest
    {
        /// <summary>
        /// 交易紀錄ID
        /// </summary>
        [Required]
        public Guid TransferRecordId { get; set; }

        /// <summary>
        /// 錢包ID
        /// </summary>
        [Required]
        public Guid WalletId { get; set; }

        /// <summary>
        /// 異動金額
        /// </summary>
        [Required]
        public decimal Amount { get; set; }
    }
}
