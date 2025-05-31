namespace DACSN10.Models
{
    public enum PaymentStatus
    {
        Pending = 0,        // Chờ thanh toán
        WaitingConfirm = 1, // Đã chuyển tiền, chờ admin xác nhận
        Success = 2,        // Đã xác nhận thành công
        Failed = 3,         // Thất bại
        Rejected = 4        // Admin từ chối
    }
}