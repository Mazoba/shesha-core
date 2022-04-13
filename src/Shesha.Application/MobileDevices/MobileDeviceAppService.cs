using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.Web.DataTable;

namespace Shesha.MobileDevices
{
    public class MobileDeviceAppService: SheshaCrudServiceBase<MobileDevice, MobileDeviceDto, Guid>
    {
        public MobileDeviceAppService(IRepository<MobileDevice, Guid> repository) : base(repository)
        {
        }

        /// <summary>
        /// Mobile Devices index table
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<MobileDevice, Guid>("MobileDevices_Index");

            table.AddProperty(e => e.Name, c => c.SortAscending());
            table.AddProperty(e => e.IMEI);
            table.AddProperty(e => e.CreatorUser.UserName, c => c.Caption("Created By"));
            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On"));
            table.AddProperty(e => e.IsLocked);

            return table;
        }
        public async Task<MobileDeviceDto> GetDeviceByEmei(string imei)
        {
            var device = await Repository.FirstOrDefaultAsync(r => r.IMEI == imei);
            return ObjectMapper.Map<MobileDeviceDto>(device);
        }
    }
}
