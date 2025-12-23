using System.Collections.Specialized;
using Prism.Regions;

namespace PrintMate.Terminal.Region;

public class AnimatedContentControlRegionAdapter : RegionAdapterBase<AnimatedContentControl>
{
    public AnimatedContentControlRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory)
        : base(regionBehaviorFactory)
    {
    }

    protected override void Adapt(IRegion region, AnimatedContentControl regionTarget)
    {
        region.ActiveViews.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    regionTarget.Content = item;
                }
            }
        };
    }

    protected override IRegion CreateRegion()
    {
        return new SingleActiveRegion();
    }
}