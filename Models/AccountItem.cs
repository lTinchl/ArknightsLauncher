namespace ArknightsLauncher.Models
{
    public class AccountItem
    {
        public string Id { get; set; }
        public string Remark { get; set; }
        public override string ToString() => Remark;
    }
}
