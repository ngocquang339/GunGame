# Cannon prototype

Mở `Assets/Scenes/CannonPrototype.unity` trong Unity rồi nhấn Play.

- Di chuột để ngắm, giữ chuột trái để bắn. Pháo đứng tại tâm và xoay tối đa ±75°.
- Enemy đỏ rơi ngẫu nhiên. Một enemy chạm đường đỏ phía dưới là thua.
- Mỗi enemy bị bắn chết cộng một kill. Nhấn R hoặc Restart khi thua.
- Bắn item xanh để nhận Triple Shot; item tím để nhận Random Spread.
- Skill mới thay thế skill cũ, nhận lại cùng skill sẽ làm mới thời hạn.
- Hết thời hạn sẽ trở lại một viên đạn. Item rơi khỏi màn hình không gây thua.

## Cấu hình

Chọn Cannon Game trong scene để chỉnh giới hạn xoay, tốc độ đạn, nhịp bắn,
nhịp spawn, tốc độ rơi và danh sách skill.
Tạo skill qua Create > Gun Game > Weapon Skill, chỉnh số viên, góc tỏa,
randomSpread, thời hạn và màu rồi thêm vào danh sách Skills.
ScriptableObject chỉ giữ cấu hình; thời gian buff được giữ riêng trong runtime.

ActorPool tạo sẵn 64 enemy, 256 đạn và 16 item. Hết pool thì bỏ qua lần spawn;
không Instantiate/Destroy liên tục trong vòng chơi. Restart trả toàn bộ object về pool.
CannonGame xử lý gameplay và va chạm dạng swept AABB, không cần Rigidbody2D,
Collider2D hay tag. Hình vuông và UI OnGUI là hình ảnh tạm của bản mẫu.
Enemy.cs có sẵn chưa được sử dụng; bước mở rộng có thể tách hành vi từng actor
và thay MakeVisual bằng prefab có sprite/animation.

Scene mẫu riêng, không thay SampleScene. Khi build, thêm CannonPrototype vào
Build Profiles > Scene List và đặt làm scene đầu tiên nếu muốn chạy bản mẫu.

## Kiểm tra thủ công

1. Kiểm tra hướng đạn theo nòng ở cả hai giới hạn xoay.
2. Bắn enemy và kiểm tra mỗi enemy chỉ cộng một kill.
3. Bắn hai loại item, kiểm tra số đạn, góc tỏa, thay thế và hết thời hạn.
4. Để enemy chạm đường đỏ: toàn bộ gameplay dừng và hiện số kill.
5. Restart nhiều lần: kill về 0, buff mất, object cũ biến mất, spawn hoạt động lại.
6. Thử Game view 16:9 và 9:16 trước khi Play.
