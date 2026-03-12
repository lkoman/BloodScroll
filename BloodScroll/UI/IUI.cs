using Microsoft.Xna.Framework;

namespace BloodScroll;

public interface IUI
{
    bool GiftCardDisplayed { get; set; }

    string GiftTitle { get; set; }
}