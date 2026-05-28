using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ECQ_Soft.Forms;

namespace ECQ_Soft.Helper
{
    public class LoadingOverlay : IDisposable
    {
        private static FrmLoadingBackdrop _backdropForm;
        private static FrmSpinner _spinnerForm;
        private static Thread _spinnerThread;
        private static readonly object _lock = new object();
        private static int _showCount = 0;
        private static Form _mainForm;

        public LoadingOverlay(Control parent, string message = "Đang tải dữ liệu...")
        {
            Show(parent, message);
        }

        public static void Show(Control parent, string message = "Đang tải dữ liệu...")
        {
            lock (_lock)
            {
                _showCount++;
                if (_showCount > 1)
                {
                    return;
                }

                _mainForm = parent?.FindForm();

                // Vô hiệu hóa form cha để chặn click/keyboard inputs
                if (_mainForm != null && !_mainForm.IsDisposed)
                {
                    try
                    {
                        if (_mainForm.InvokeRequired)
                        {
                            _mainForm.Invoke(new Action(() => _mainForm.Enabled = false));
                        }
                        else
                        {
                            _mainForm.Enabled = false;
                        }
                    }
                    catch { }
                }

                // Tính toán kích thước (Bounds) toàn màn hình chứa form cha trên luồng UI chính
                Rectangle bounds = Rectangle.Empty;
                if (_mainForm != null && !_mainForm.IsDisposed)
                {
                    try
                    {
                        if (_mainForm.InvokeRequired)
                        {
                            _mainForm.Invoke(new Action(() => { bounds = Screen.FromControl(_mainForm).Bounds; }));
                        }
                        else
                        {
                            bounds = Screen.FromControl(_mainForm).Bounds;
                        }
                    }
                    catch
                    {
                        bounds = Screen.PrimaryScreen.Bounds;
                    }
                }
                else
                {
                    bounds = Screen.PrimaryScreen.Bounds;
                }

                // Khởi chạy Spinner Form trên một luồng phụ riêng biệt (Background UI Thread)
                _spinnerThread = new Thread(() =>
                {
                    _backdropForm = new FrmLoadingBackdrop(bounds);
                    _spinnerForm = new FrmSpinner(bounds, message);
                    _backdropForm.Show();
                    _spinnerForm.Show();
                    Application.Run();
                });
                _spinnerThread.SetApartmentState(ApartmentState.STA);
                _spinnerThread.IsBackground = true;
                _spinnerThread.Start();
            }
        }

        public static void Hide()
        {
            lock (_lock)
            {
                if (_showCount <= 0) return;
                _showCount--;
                if (_showCount > 0) return;

                // Kích hoạt lại form cha
                if (_mainForm != null && !_mainForm.IsDisposed)
                {
                    try
                    {
                        if (_mainForm.InvokeRequired)
                        {
                            _mainForm.Invoke(new Action(() =>
                            {
                                _mainForm.Enabled = true;
                                _mainForm.Activate();
                            }));
                        }
                        else
                        {
                            _mainForm.Enabled = true;
                            _mainForm.Activate();
                        }
                    }
                    catch { }
                }

                // Tắt Spinner Form
                var formToClose = _spinnerForm;
                var backdropToClose = _backdropForm;
                var invokeForm = (Form)formToClose ?? backdropToClose;
                if (invokeForm != null)
                {
                    try
                    {
                        if (invokeForm.IsHandleCreated)
                        {
                            invokeForm.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    if (formToClose != null && !formToClose.IsDisposed)
                                    {
                                        formToClose.Close();
                                    }

                                    if (backdropToClose != null && !backdropToClose.IsDisposed)
                                    {
                                        backdropToClose.Close();
                                    }

                                    Application.ExitThread();
                                }
                                catch { }
                            }));
                        }
                    }
                    catch { }
                    _backdropForm = null;
                    _spinnerForm = null;
                }

                _spinnerThread = null;
                _mainForm = null;
            }
        }

        public void Dispose()
        {
            Hide();
        }
    }
}
