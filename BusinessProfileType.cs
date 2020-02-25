using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using SmartAnalytics.HonestWine.DAL.EF;
using SmartAnalytics.HonestWine.PIM.CustomType.SalesList.Query;
using SmartAnalytics.HonestWine.PIM.CustomType.WishList.Query;
using SmartAnalytics.HonestWine.PIM.EntityExtensions;
using System;
using System.Linq;

namespace SmartAnalytics.HonestWine.PIM.CustomType.Profiles
{
    public class BusinessProfileType : ObjectGraphType<DAL.EF.Entities.ApplicationBusiness>
    {
        public BusinessProfileType(IDataLoaderContextAccessor accessor, IServiceProvider services)
        {
            Field(x => x.Id);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<SalesListItemType>>>>()
                .Name("salesList")
                .ResolveAsync(async context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, DAL.EF.Entities.SalesListItem>("GetSalesList", async (businessIds) =>
                    {
                        using (var scope = services.CreateScope())
                        {
                            var repository = scope.ServiceProvider.GetService<IHonestWineRepository>();
                            var salesList = await repository.QuerySalesListByBusinessId(businessIds.ToArray())
                                .ToAsyncEnumerable()
                                .ToLookup(x => x.ApplicationBusinessId, x => x);
                            return salesList;
                        }
                    });

                    return await loader.LoadAsync(context.Source.Id);
                });

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<WishListItemType>>>>()
                .Name("wishList")
                .ResolveAsync(async context =>
                {
                    var loader = accessor.Context.GetOrAddCollectionBatchLoader<int, DAL.EF.Entities.WishListItem>("GetWishList", async (businessIds) =>
                    {
                        using (var scope = services.CreateScope())
                        {
                            var repository = scope.ServiceProvider.GetService<IHonestWineRepository>();
                            var wishList = await repository.QueryWishListByBusinessId(businessIds.ToArray())
                                .ToAsyncEnumerable()
                                .ToLookup(x => x.ApplicationBusinessId, x => x);
                            return wishList;
                        }
                    });

                    return await loader.LoadAsync(context.Source.Id);
                });
        }
    }
}
