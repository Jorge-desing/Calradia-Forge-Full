namespace CalradiaForge.PriceContracts
{
    // Both modules reference this assembly. Only the provider distributes the DLL.
    public interface IPriceCalculator
    {
        int CalculateTotal(int unitPrice, int quantity);
    }
}
