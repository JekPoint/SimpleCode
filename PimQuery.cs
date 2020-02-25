using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartAnalytics.HonestWine.DAL.EF;
using SmartAnalytics.HonestWine.PIM.CustomType.AdminType.Query;
using SmartAnalytics.HonestWine.PIM.CustomType.CatalogType.Query;
using SmartAnalytics.HonestWine.PIM.CustomType.Currency;
using SmartAnalytics.HonestWine.PIM.CustomType.OwnerType.Query;
using SmartAnalytics.HonestWine.PIM.CustomType.Profiles;
using SmartAnalytics.HonestWine.PIM.CustomType.SalesList.Query;
using SmartAnalytics.HonestWine.PIM.Helpers;
using System;
using GraphQL.Authorization;

namespace SmartAnalytics.HonestWine.PIM
{
    [GraphQLAuthorize(Policy = "BlockedPolicy")]
    public class PimQuery : ObjectGraphType
    {
        
        public PimQuery(IServiceProvider services)
        {
            Name = "Query";

            Field<OwnerType>("owner", "Работа с персональными винами/продуктами", resolve: context => new { });
            Field<AdminType>("admin", "Работа с видением WWL", resolve: context => new { });
            Field<CatalogType>("catalog", "Справочники доступные для чтения", resolve: context => new { });

            Field<ApplicationUserType>()
                .Name("currentUser")
                .Description("Работа от текущего пользователя")
                .ResolveAsync(async context =>
                {
                    using (var scope = services.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetService<IHonestWineRepository>();
                        var user = await repository.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == context.GetUserId());
                        return user;
                    }
                });

            Field<ListGraphType<NonNullGraphType<SalesListUnitType>>>()
                .Name("salesListUnits")
                .Description("Измерения для списка продаж")
                .ResolveAsync(async context =>
                {
                    using (var scope = services.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetService<IHonestWineRepository>();
                        var units = await repository.SalesListUnits.ToListAsync();
                        return units;
                    }
                });


            Field<ListGraphType<NonNullGraphType<CurrencyType>>>()
                .Name("currencies")
                .Description("Валюты")
                .ResolveAsync(async context =>
                {
                    using (var scope = services.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetService<IHonestWineRepository>();
                        var currencies = await repository.Currencies.ToListAsync();
                        return currencies;
                    }
                });

        }
    }
}