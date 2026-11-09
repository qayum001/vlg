namespace Vlg.Core.Run;

/// <param name="Item">Index into ItemsConfig.Items.</param>
public readonly record struct ShopOffer(int Item, float Price, bool Sold);
