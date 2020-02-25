using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartAnalytics.HonestWine.DAL.EF;
using SmartAnalytics.HonestWine.DAL.EF.Entities;
using SmartAnalytics.HonestWine.DAL.EF.Helpers;
using SmartAnalytics.HonestWine.PIM.CustomType.OwnerType.Model.SearchModels;

namespace SmartAnalytics.HonestWine.PIM.Services
{
    /// <summary>
    /// Service for obtaining specific directories
    /// </summary>
    public class CatalogService : IDisposable, ICatalogService
    {
        private readonly IHonestWineRepository _repository;

        /// <summary>
        /// Constructor, include EF IHonestWineRepository
        /// </summary>
        /// <param name="repository"></param>
        public CatalogService(IHonestWineRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Get localized list Sweetness Level by wine type
        /// </summary>
        /// <param name="localId">locale id</param>
        /// <param name="wineTypeId">wine type</param>
        /// <returns>List Sweetness Level by wine type</returns>
        public async Task<IEnumerable<SweetnessLevelLocale>> GetSweetnessLevelByWineTypeAsync(int localId, int? wineTypeId)
        {
            var sweetnessLevelIdByWineType = wineTypeId != null 
                ? await _repository.WineTypeSubtypeSweetnessLevels.Where(x => x.WineTypeId == wineTypeId).ToListTryAsync()
                : await _repository.WineTypeSubtypeSweetnessLevels.ToListTryAsync();

            return await _repository.SweetnessLevelLocales
                .Where(sweetnessLevelLocale =>  sweetnessLevelLocale.LocaleId == localId &&
                sweetnessLevelIdByWineType.Any(wineTypeSubtypeSweetnessLevel => 
                        wineTypeSubtypeSweetnessLevel.SweetnessLevelId == sweetnessLevelLocale.SweetnessLevelId)).ToListTryAsync();
        }

        /// <summary>
        /// Get Filters of WineColors, Grapes, Ranges, Vintages, for current business profile Wish list
        /// </summary>
        /// <param name="localId">Current local id for naming</param>
        /// <param name="profileId">Current user profile id</param>
        /// <returns>Filter parameter of WineColors, Grapes, Ranges, Vintages by all product in list</returns>
        public async Task<OwnerFilters> GetWishListFiltersAsync(int localId, int profileId)
        {
            var businessProfile = await GetBusinessProfileAsync(profileId);

            if (businessProfile == null)
            {
                return null;
            }

            var currentWishListWines = await _repository.WishListItems
                .Include(x => x.Product)
                .ThenInclude(x => x.Bottle)
                .ThenInclude(x => x.Wine)
                .Where(x => x.ApplicationBusinessId == businessProfile.Id && !x.Product.RecordIsDeleted)
                .Select(x => x.Product.Bottle.Wine)
                .ToListTryAsync();

            return await GetFiltersByWinesAsync(currentWishListWines, localId);
        }
        
        /// <summary>
        /// Get Filters of WineColors, Grapes, Ranges, Vintages, for current business profile Sales list
        /// </summary>
        /// <param name="localId">Current local id for naming</param>
        /// <param name="profileId">Current user profile id</param>
        /// <returns>Filter parameter of WineColors, Grapes, Ranges, Vintages by all product in list</returns>
        public async Task<OwnerFilters> GetSalesListFiltersAsync(int localId, int profileId)
        {
            var businessProfile = await GetBusinessProfileAsync(profileId);

            if (businessProfile == null)
            {
                return null;
            }

            var currentSalesListWines = await _repository.SalesListItems
                .Include(x=>x.Product)
                .ThenInclude(x => x.Bottle)
                .ThenInclude(x => x.Wine)
                .Where(x => x.ApplicationBusinessId == businessProfile.Id && !x.Product.RecordIsDeleted)
                .Select(x => x.Product.Bottle.Wine)
                .ToListTryAsync();

            return await GetFiltersByWinesAsync(currentSalesListWines, localId);
        }

        private async Task<ApplicationBusiness> GetBusinessProfileAsync(int profileId)
        {
            var businessProfile = await _repository.ApplicationProfiles
                .Include(x => x.ApplicationBusiness)
                .Where(x => x.Id == profileId)
                .Select(x => x.ApplicationBusiness)
                .FirstOrDefaultTryAsync();

            return businessProfile;
        }

        private async Task<OwnerFilters> GetFiltersByWinesAsync(List<DAL.EF.Entities.Wine> wines, int localId)
        {
            var wineColorFilters = await _repository.WineColorLocales
                .Where(x => wines.Exists(wine => wine.WineColorId == x.WineColorId) && x.LocaleId == localId)
                .Select(x => new Filter
                {
                    Id = x.WineColorId,
                    Name = x.WineColorName
                })
                .ToListTryAsync();

            var grapesFilter = await _repository.Blends
                .Include(x => x.Grape)
                .Join(this._repository.GrapeLocales, blend => blend.GrapeId, grapeLocal => grapeLocal.GrapeId,
                    (blend, grapeLocal) => new { blend, grapeLocal })
                .Where(x => x.blend.GrapeId == x.grapeLocal.GrapeId && x.grapeLocal.LocaleId == localId && wines.Exists(wine => x.blend.WineId == wine.Id))
                .Select(x => new Filter
                {
                    Id = x.blend.GrapeId,
                    Name = x.grapeLocal.GrapeName
                })
                .ToListTryAsync();

            var rangesFilter = await _repository.WineRanges
                .Where(x => wines.Exists(wine => wine.WineRangeId == x.Id))
                .Select(x => new Filter
                {
                    Id = x.Id,
                    Name = x.WineRangeName
                })
                .ToListTryAsync();

            var vintagesFilter = await _repository.VintageLocales
                .Where(x => wines.Exists(wine => wine.VintageId == x.VintageId) && x.LocaleId == localId)
                .Select(x => new Filter
                {
                    Id = x.VintageId,
                    Name = x.VintageValue
                })
                .ToListTryAsync();

            return new OwnerFilters
            {
                WineColors = wineColorFilters,
                Grapes = grapesFilter,
                Ranges = rangesFilter,
                Vintages = vintagesFilter
            };
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
