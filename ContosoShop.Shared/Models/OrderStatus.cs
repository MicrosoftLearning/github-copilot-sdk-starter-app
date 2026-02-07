namespace ContosoShop.Shared.Models;

/// <summary>
/// Represents the current state of an order in its lifecycle.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order received, preparing for shipment
    /// </summary>
    Processing = 0,

    /// <summary>
    /// Order handed to carrier, in transit
    /// </summary>
    Shipped = 1,

    /// <summary>
    /// Order delivered to customer
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Customer initiated return, refund processed
    /// </summary>
    Returned = 3
}
