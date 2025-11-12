/********************************************************************
生成日期:	11:11:2025
类    名: 	HudNumber
作    者:	HappLI
描    述:	数字
*********************************************************************/
namespace Framework.HUD.Runtime
{
    public class HudNumber : AComponent
    {
        public HudNumber(HudSystem pSystem) : base(pSystem)
        {
            m_eHudType = EHudType.Number;
        }
    }
}
