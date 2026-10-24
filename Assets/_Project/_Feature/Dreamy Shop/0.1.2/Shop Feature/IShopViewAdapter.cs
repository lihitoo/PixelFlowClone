using UnityEngine;

namespace Dreamy.Feature.Shop.Integration
{
    /// <summary>Optional native game skin; ShopPanel retains UIPanel/presenter ownership.</summary>
    public interface IShopViewAdapter : Dreamy.Shop.IShopView
    {
        Transform RewardSource { get; }
    }
}
