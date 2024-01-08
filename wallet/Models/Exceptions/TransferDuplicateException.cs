namespace wallet.Models.Exceptions
{
    public class TransferDuplicateException(Guid transferRecordId) : Exception
    {

        public override string Message => $"Transfer Record Id Duplicate! TransferRecordId:{TransferRecordId}";

        public Guid TransferRecordId => transferRecordId;
    }
}
