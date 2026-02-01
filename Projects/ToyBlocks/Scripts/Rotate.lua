
--あいうえおアイウエオ亜伊宇絵尾
local Spr = self:findComponent({"Sprite2D"}) --[[@as Sprite2D]]--
print("Sprite component:", Spr)
function loop()
    --if time.passed < 3.5 then return end
    Spr.rotation = Spr.rotation + 1.0
    if Spr.rotation > 360 then Spr.rotation = 0 end
end