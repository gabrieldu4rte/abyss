using System.Collections.Generic;

namespace Abyss.Domain;
internal sealed class MerchantState
{
    internal List<Offer> MerchantStock { get; } = new();
}
