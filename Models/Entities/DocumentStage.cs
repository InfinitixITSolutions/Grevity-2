namespace Grevity.Models.Entities
{
    public enum DocumentStage
    {
        Draft,
        Quotation,
        Order,
        Invoice, // This represents the final "Confirmed" stage (Invoice/Bill)
        Cancelled
    }
}
