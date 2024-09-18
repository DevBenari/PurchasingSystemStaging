namespace PurchasingSystemApps.Areas.MasterData.ViewModels
{
    public class PrincipalViewModel
    {
        public Guid PrincipalId { get; set; }
        public string PrincipalCode { get; set; }
        public string PrincipalName { get; set; }
        public string Address { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public string? Note { get; set; }
    }
}
