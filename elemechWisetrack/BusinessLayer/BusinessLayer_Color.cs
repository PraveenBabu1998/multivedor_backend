using Azure.Core;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Color
    {
        Task<object> AddColors(string userEmail, [FromBody] ProductsCollors request);
        Task<object> GetColors();
        Task<object> UpdateColor(string userEmail, Guid id, ProductsCollors request);
        Task<object> ToggleColorStatus(Guid id);
        Task<object> SoftDeleteColor(Guid id);
        Task<object> RestoreColor(Guid id);
        Task<object> DeleteColor(Guid id);
    }
    public partial interface IBusinessLayer : IBusinessLayer_Color
    {

    }

    public partial class BusinessLayer
    {
        public async Task<object> AddColors(string userEmail,[FromBody] ProductsCollors request)
        {
            string baseSlug = GenerateSlugProduct(request.Name);
            return await _dataBaseLayer.AddColors(userEmail,request, baseSlug);
        }

        public async Task<object> GetColors()
        {
            return await _dataBaseLayer.GetColors();
        }

        public async Task<object> UpdateColor(string userEmail, Guid id, ProductsCollors request)
        {
            string baseSlug = GenerateSlugProduct(request.Name);
            return await _dataBaseLayer.UpdateColor(userEmail, id, request, baseSlug);
        }
        public async Task<object> ToggleColorStatus(Guid id)
        {
            return await _dataBaseLayer.ToggleColorStatus(id);
        }

        public async Task<object> SoftDeleteColor(Guid id)
        {
            return await _dataBaseLayer.SoftDeleteColor(id);
        }

        public async Task<object> RestoreColor(Guid id)
        {
            return await _dataBaseLayer.RestoreColor(id);
        }

        public async Task<object> DeleteColor(Guid id)
        {
            return await _dataBaseLayer.DeleteColor(id);
        }

    }
}
