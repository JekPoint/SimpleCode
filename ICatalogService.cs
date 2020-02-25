using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAnalytics.HonestWine.DAL.EF.Entities;
using SmartAnalytics.HonestWine.PIM.CustomType.OwnerType.Model.SearchModels;

namespace SmartAnalytics.HonestWine.PIM.Services
{
    public interface ICatalogService
    {
        /// <summary>
        /// Get localized list Sweetness Level by wine type
        /// </summary>
        /// <param name="localId">locale id</param>
        /// <param name="wineTypeId">wine type</param>
        /// <returns>List Sweetness Level by wine type</returns>
        Task<IEnumerable<SweetnessLevelLocale>> GetSweetnessLevelByWineTypeAsync(int localId, int? wineTypeId);

        /// <summary>
        /// Get Filters of WineColors, Grapes, Ranges, Vintages, for current business profile Wish list
        /// </summary>
        /// <param name="localId">Current local id for naming</param>
        /// <param name="profileId">Current user profile id</param>
        /// <returns>Filter parameter of WineColors, Grapes, Ranges, Vintages by all product in list</returns>
        Task<OwnerFilters> GetWishListFiltersAsync(int localId, int profileId);

        /// <summary>
        /// Get Filters of WineColors, Grapes, Ranges, Vintages, for current business profile Sales list
        /// </summary>
        /// <param name="localId">Current local id for naming</param>
        /// <param name="profileId">Current user profile id</param>
        /// <returns>Filter parameter of WineColors, Grapes, Ranges, Vintages by all product in list</returns>
        Task<OwnerFilters> GetSalesListFiltersAsync(int localId, int profileId);
    }
}