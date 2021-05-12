using System.ComponentModel;
using Shesha.Domain.Attributes;

namespace Shesha.Domain.Enums
{
    [ReferenceList("Shesha.Core", "OrderStatus")]
    public enum RefListOrderStatus: long
    {
        [Description("Order Received")]
        OrderReceived = 1,

        [Description("Ready for Collection")]
        ReadyForCollection = 2,

        [Description("Order Cancelled")]
        OrderCancelled = 3,

        [Description("Awaiting Supplier Delivery")]
        AwaitingSupplierDelivery = 4,

        [Description("Order submitted")]
        OrderSubmitted = 5,

        [Description("Draft")]
        Draft = 6,

        [Description("Completed")]
        Completed = 7
    }
}
