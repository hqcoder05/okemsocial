import os
import glob

replacements = {
    "Ä Äƒng kÃ½": "Đăng ký",
    "Ä Äƒng nháº\xadp": "Đăng nhập",
    "Táº¡o tÃ\xa0i khoáº£n": "Tạo tài khoản",
    "Tham gia káº¿t ná»‘i cÃ¹ng Okem ngay hÃ´m nay": "Tham gia kết nối cùng Okem ngay hôm nay",
    "Máº\xadt kháº©u": "Mật khẩu",
    "XÃ¡c nháº\xadn máº\xadt kháº©u": "Xác nhận mật khẩu",
    "TÃªn hiá»ƒn thá»‹": "Tên hiển thị",
    "TÃªn Ä‘áº§y Ä‘á»§": "Tên đầy đủ",
    "Ä á»‹a chá»‰ Email": "Địa chỉ Email",
    "Hoáº·c Ä‘Äƒng kÃ½ báº±ng": "Hoặc đăng ký bằng",
    "Hoáº·c tiáº¿p tá»¥c vá»›i": "Hoặc tiếp tục với",
    "Ä Ã£ cÃ³ tÃ\xa0i khoáº£n?": "Đã có tài khoản?",
    "HÃ£y Ä‘Äƒng nháº\xadp ngay": "Hãy đăng nhập ngay",
    "ChÆ°a cÃ³ tÃ\xa0i khoáº£n?": "Chưa có tài khoản?",
    "Báº£ng tin": "Bảng tin",
    "Máº¡ng lÆ°á»›i": "Mạng lưới",
    "Tin nháº¯n": "Tin nhắn",
    "CÃ\xa0i Ä‘áº·t": "Cài đặt",
    "TÃ¬m kiáº¿m báº¡n bÃ¨, bÃ\xa0i viáº¿t...": "Tìm kiếm bạn bè, bài viết...",
    "Há»“ sÆ¡ cÃ¡ nhÃ¢n": "Hồ sơ cá nhân",
    "Ä Äƒng xuáº¥t": "Đăng xuất",
    "Chá»§ Ä‘á»  thá»‹nh hÃ\xa0nh": "Chủ đề thịnh hành",
    "LiÃªn há»‡": "Liên hệ",
    "Báº¡n Ä‘ang nghÄ© gÃ¬?": "Bạn đang nghĩ gì?",
    "ThÃ´ng bÃ¡o": "Thông báo",
    "ChÆ°a cÃ³ tin nháº¯n": "Chưa có tin nhắn",
    "Trá»±c tuyáº¿n": "Trực tuyến",
    "Gá» i thoáº¡i": "Gọi thoại",
    "Gá» i video": "Gọi video",
    "Nháº\xadp tin nháº¯n...": "Nhập tin nhắn..."
}

for filepath in glob.glob("d:/okemsocial/Views/**/*.cshtml", recursive=True):
    with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
        text = f.read()
    
    changed = False
    for k, v in replacements.items():
        if k in text:
            text = text.replace(k, v)
            changed = True
            
    if changed:
        with open(filepath, "w", encoding="utf-8-sig") as f:
            f.write(text)
print("Done fixing mojibake!")
