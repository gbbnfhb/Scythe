
--あいうえおアイウエオ亜伊宇絵尾
local Spr = self:findComponent({"Sprite2D"}) --[[@as Sprite2D]]--
local pos = self.transform.pos
local step = 1.0;
print("Sprite component:", Spr)
function loop()
    --if time.passed < 3.5 then return end
    Spr.rotation = Spr.rotation + 1.0
    if Spr.rotation > 360 then Spr.rotation = 0 end
    pos.x = pos.x + step
    if pos.x > 200 then step = -1.0 end
    if pos.x < 0 then step = 1.0 end
    self.transform.pos = pos
end