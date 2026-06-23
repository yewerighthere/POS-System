using System.Globalization;
using System.Windows.Data;

namespace SmartPOS.WPF.Converters;

public class ProductImageConverter : IValueConverter
{
    private static readonly string DefaultImage = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=400"; // Beautiful generic food shot

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string sku || string.IsNullOrWhiteSpace(sku))
            return DefaultImage;

        sku = sku.ToUpperInvariant();

        if (sku.StartsWith("BM-")) // Bánh mì
            return "https://lh3.googleusercontent.com/aida-public/AB6AXuBbIYOH79onY6YS4qyeFnBdvzjkYY9FPU0Mtgo_HpNaCwTxfFRxtJyMbVPwYqmBulSOe_Ot2PrNS4RVS2BYdhp5MBa9qcoHA8mx2TrzMlPIF_FsbWpp4FK_xIs0rSpX43e7aMbsig7IaOwmg-SYhXSk-F6-FfhYYsC7g7KzBYYpJydN22LEVpIJrJNEZZCfpxw8ntYjuYP79jJY7Lx6gNgEks8Mu5ZbaRh5RdW9WZBic0Gi1Go7KxNyWl599nmlSyU1ITV3hxhGzJA";
        if (sku.StartsWith("CF-") || sku.StartsWith("TS-") || sku.Equals("NUOC-CAM")) // Drink/Coffee
            return "https://lh3.googleusercontent.com/aida-public/AB6AXuC0Dn4vP3f6lR6ml16RYF85MdtatvDmmNwTKUtqhxyBLf7cWgblX9ofVSo6C780AZGcW2dqWAsTWlY2Pc6oiJLDHqmUbXxVxgkZ_2zep83dpG3Xwu2s851SgY6TklAMa3cbAYvYzdmPbzVM_vHP4-y6jyzRo9yz-r_NeIe1RyvOBZvM6jWzKzOVwkUj57x8oq7WTuqXCCUZsydM723MDpKW_NoqEohN7t64FMTeTfi1RW1q6QVLH8D2y48WsuviR0f8cyiRa_QZPQM";
        if (sku.Equals("VM-MK010")) // Vinamilk
            return "https://lh3.googleusercontent.com/aida-public/AB6AXuBjp-PaqzxFy_ZNTeHxKjNibxzdCjMMHV1ugB9yakrr2O4hDA04wXhfrbe8bk7-0EHfz4hqIVekR4Ov5zihrm0jaY8D_QgMAeuzQycQxsCocXnkeuK4AH4tezdXXAdgGa9uRD3bq0ft91yUFh-KQHXioCsRTTTlqw46Y21B_qpVS188OnDXLCPkt1eIdZLsE_zwtAgU2eV0gM-PfVkKtIisKtM4O7FVDH1l_Svhc0Qaevyk0E8btFXkl_e1cMcbmukNkYGIutgKP3c";
        if (sku.Equals("LS-PK95")) // Lays
            return "https://lh3.googleusercontent.com/aida-public/AB6AXuCJDTBMl_XkRYRPfY08mFXuN2JvKUlRhxn5kiHQoiJji2IkzcueCJo8kM4LsCBDlm6MPjQt9Uron_wuDsiR3DmEoPyD6t9w1uHVnZauGCRxJnTMVIQ724acbaw38B80yJseVF0kFnM7y2O-deM7kbiUdP-C3QTSU5TtHoJwXZ4z2986DhGBbT1e1D3NSVMzlhGbmZ9H_uwro6bpG9CGgiLMN43BIDefNju_3Ty-5Zd0Lz1uFVT3hP4meHDB-f6P9iJ2YO87tmqmoxg";
        if (sku.Equals("AQ-500")) // Aquafina
            return "https://lh3.googleusercontent.com/aida-public/AB6AXuCngV4sv2a_ubk5F9TozPk7FQxJ6SKEkb5HXf9ywyON4p86AiazULpW2itE9srM4r8-H-wf25o1C_EJvfE2lx1hZQ7rx6Nhb6bAUfEbC594JiSkLCRbm7jrunVkiVuPphsG1qz2ycbVwY1VxwhpIqmGV0OnwJn61K9CYt83nZiaMi79sa8FtDBvzM-sSEnVvYEjbzdD5AYZwCUqpcYYg24jQkN0-DQEUJyJu3vFR9w6HJyR20f9cHL9iazn2F4Q7I_fnP0wNbl2k9g";
        if (sku.Equals("HH-TCC01")) // Hao Hao
            return "https://lh3.googleusercontent.com/aida-public/AB6AXuC5grT_ySBpFqs2yv2yNjD4Kb141jwqz3YYdsbkA8-NMXxeqcQy4PlnrGts2ZoIL5VRKun5bTr5-j0qjiGLWwrdPLGLvzWgwzSj_qkQgipagjzEtCvcmF14o-Cwnwj6V782LjYDe9f8GuUOT8Bupej_X-fpo1nJFgjNS_PJy2qidHUHifwOdbbWxx_ni4kCrk5Q9pxHP43A_bkXLUggfwB75vrvyatxQ_XVbUx4cvQV-1kApsV5NcZogWH5MZYbqMvzNDTb9zbDH-E";
        if (sku.StartsWith("BANH-") || sku.StartsWith("CHE-")) // Desserts
            return "https://images.unsplash.com/photo-1551024601-bec78aea704b?w=400"; // Dessert

        return DefaultImage;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
