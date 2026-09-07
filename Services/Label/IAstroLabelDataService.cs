
using SVN_Tools.Models.Label;
using SVN_Tools.Models.Utils;
public interface IAstroLabelDataService
{
    Task<AstroLabelData> CreateAsync(AstroLabelData data);
    Task<AstroLabelData> GetByPalletIdAsync(string palletID);
    Task<IEnumerable<AstroLabelData>> GetAllAsync();
    Task<AstroLabelData?> UpdateAsync(AstroLabelData data);
    Task<bool> DeleteAsync(int id);
    Task<List<AstroLabelData>> GetPackageListByPrefix(string prefix);

    Task<bool> checkSerialExist(string prefix, string serial);

    Task<bool> DeletePackageId(string PackageID);

    Task<int> CountPalletID(string palletID);

    Task<bool> PrintPallet(string palletID);
    Task<bool> PrintPalletWithBody(string palletID, LabelDataRequest request);
    Task<List<AstroLabelData>> GetPallet(string palletID);
    Task<bool> DeleteBox(string serial);
}