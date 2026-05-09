namespace MediStock.Domain.Exceptions;

public class InsufficientStockException : Exception
{
    public int DrugId { get; }

    public InsufficientStockException(int drugId)
        : base($"Insufficient stock for drug Id {drugId}.")
    {
        DrugId = drugId;
    }
}
