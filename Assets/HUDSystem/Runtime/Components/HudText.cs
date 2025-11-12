/********************************************************************
生成日期:	11:11:2025
类    名: 	HudText
作    者:	HappLI
描    述:	文字
*********************************************************************/
namespace Framework.HUD.Runtime
{
    [HudData(typeof(HudTextData))]
    public class HudText : AComponent
    {
        public HudText(HudSystem pSystem) : base(pSystem)
        {
            m_eHudType = EHudType.Text;
        }
    }
}
