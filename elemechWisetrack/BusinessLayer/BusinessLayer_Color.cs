using Azure.Core;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Color
    {
        Task<object> AddColors(string userEmail, [FromBody] ProductsCollors request);
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


    }
}
