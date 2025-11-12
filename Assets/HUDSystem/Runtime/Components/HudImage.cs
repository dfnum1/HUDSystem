/********************************************************************
生成日期:	11:11:2025
类    名: 	HudImage
作    者:	HappLI
描    述:	图片
*********************************************************************/

namespace Framework.HUD.Runtime
{
    [HudData(typeof(HudImageData))]
    public class HudImage : AComponent
    {
        public HudImage(HudSystem pSystem) : base(pSystem)
        {
            m_eHudType = EHudType.Image;
        }
    }
}
