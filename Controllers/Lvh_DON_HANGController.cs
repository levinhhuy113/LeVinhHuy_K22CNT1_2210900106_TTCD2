using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using LEVINHHUY_K22CNT1_2210900106.Models;

namespace LEVINHHUY_K22CNT1_2210900106.Controllers
{
    public class Lvh_DON_HANGController : Controller
    {
        private LEVINHHUY_K22CNT1_2210900106Entities3 db = new LEVINHHUY_K22CNT1_2210900106Entities3();

        // GET: DON_HANG
        public ActionResult Index()
        {
            var donHangList = db.DON_HANG.Include(d => d.KHACH_HANG).Include(d => d.SAN_PHAM);
            return View(donHangList.ToList());
        }

        // GET: DON_HANG/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DON_HANG donHang = db.DON_HANG.Find(id);
            if (donHang == null)
            {
                return HttpNotFound();
            }
            return View(donHang);
        }

        // GET: DON_HANG/Create
        // Trong phương thức Create:
        public ActionResult Create()
        {
            ViewBag.MaKH = new SelectList(db.KHACH_HANG, "MaKH", "Ho_ten");
            ViewBag.MaSP = new SelectList(db.SAN_PHAM, "MaSP", "TenSP");
            return View();
        }



        // Tạo một hàm để tạo mã đơn hàng
        private string GenerateMaDH()
        {
            var maxMaDH = db.DON_HANG.Select(d => d.MaDH).DefaultIfEmpty("DH00000000").Max();
            int nextId = int.Parse(maxMaDH.Substring(2)) + 1;
            return "DH" + nextId.ToString("D8");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaDH,MaKH,Ngay_dat,Trang_thai")] DON_HANG donHang, int MaSP, int SoLuong)
        {
            if (ModelState.IsValid)
            {
                var sanPham = db.SAN_PHAM.Find(MaSP);
                if (sanPham == null)
                {
                    ModelState.AddModelError("", "Sản phẩm không tồn tại.");
                    return View(donHang);
                }
                if (donHang.Ngay_dat < new DateTime(1753, 1, 1) || donHang.Ngay_dat > DateTime.Now)
                {
                    ModelState.AddModelError("Ngay_dat", "Ngày đặt không hợp lệ.");
                    return View(donHang);
                }

                donHang.MaDH = GenerateMaDH();
                donHang.Tong_tien = CalculateTotalPrice(sanPham.Gia, SoLuong);

                try
                {
                    db.DON_HANG.Add(donHang);
                    db.SaveChanges();

                    var chiTietDonHang = new CHI_TIET_DON_HANG
                    {
                        MaDH = donHang.MaDH,
                        MaSP = MaSP,
                        So_luong = SoLuong,
                        Gia = sanPham.Gia,
                        Tong_tien = donHang.Tong_tien
                    };

                    db.CHI_TIET_DON_HANG.Add(chiTietDonHang);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    var innerException = ex.InnerException?.InnerException?.Message ?? ex.Message;
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + innerException);
                }
            }

            ViewBag.MaKH = new SelectList(db.KHACH_HANG, "MaKH", "Ho_ten", donHang.MaKH);
            ViewBag.MaSP = new SelectList(db.SAN_PHAM, "MaSP", "TenSP");
            return View(donHang);
        }

        // Tính tổng tiền
        private decimal CalculateTotalPrice(decimal unitPrice, int quantity)
        {
            return unitPrice * quantity;
        }

        // GET: DON_HANG/Edit/5
        // GET: DON_HANG/Edit/5
        public ActionResult Edit(string id)
        {
            var order = db.DON_HANG.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.MaKH = new SelectList(db.KHACH_HANG, "MaKH", "Ho_ten", order.MaKH);
            ViewBag.MaSP = new SelectList(db.SAN_PHAM, "MaSP", "TenSP", order.MaSP);

            return View(order);
        }



        // POST: DON_HANG/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaDH,MaKH,Ngay_dat,Tong_tien,Trang_thai")] DON_HANG donHang, int MaSP, int SoLuong)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra giá trị Ngày đặt
                if (donHang.Ngay_dat < new DateTime(1753, 1, 1) || donHang.Ngay_dat > DateTime.Now)
                {
                    ModelState.AddModelError("Ngay_dat", "Ngày đặt không hợp lệ.");
                    return View(donHang);
                }

                var existingOrder = db.DON_HANG.Include("CHI_TIET_DON_HANG").FirstOrDefault(o => o.MaDH == donHang.MaDH);
                if (existingOrder == null)
                {
                    return HttpNotFound();
                }

                existingOrder.MaKH = donHang.MaKH;
                existingOrder.Ngay_dat = donHang.Ngay_dat;
                existingOrder.Trang_thai = donHang.Trang_thai;

                var sanPham = db.SAN_PHAM.Find(MaSP);
                if (sanPham != null)
                {
                    donHang.Tong_tien = CalculateTotalPrice(sanPham.Gia, SoLuong);
                    existingOrder.Tong_tien = donHang.Tong_tien;
                }

                try
                {
                    db.Entry(existingOrder).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    var innerException = ex.InnerException?.InnerException?.Message ?? ex.Message;
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật: " + innerException);
                }
            }

            ViewBag.MaKH = new SelectList(db.KHACH_HANG, "MaKH", "Ho_ten", donHang.MaKH);
            ViewBag.MaSP = new SelectList(db.SAN_PHAM, "MaSP", "TenSP", MaSP);

            return View(donHang);
        }






        // GET: DON_HANG/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DON_HANG donHang = db.DON_HANG.Find(id);
            if (donHang == null)
            {
                return HttpNotFound();
            }
            return View(donHang);
        }

        // POST: DON_HANG/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            DON_HANG donHang = db.DON_HANG.Find(id);
            db.DON_HANG.Remove(donHang);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
