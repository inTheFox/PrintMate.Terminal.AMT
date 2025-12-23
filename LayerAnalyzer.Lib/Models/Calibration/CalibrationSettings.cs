using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace LayerAnalyzer.Lib.Models.Calibration
{
    public class CalibrationSettings
    {
        public Mat CameraMatrix { get; }
        public Mat DistCoeffs { get; }
        public Mat MapX { get; }
        public Mat MapY { get; }
        public Mat TForm { get; }
        public Size FrameSizePx { get; }
        public SizeF FrameSizeMm { get; }
        public Rectangle Roi { get; }
        public bool IsCalibrationValid { get; }
        public bool IsWarpValid { get; }

        //default
        public CalibrationSettings(Mat cameraMatrix, Mat distCoeffs, Mat mapX, Mat mapY, Mat tForm, Size frameSizePx, SizeF frameSizeMm, Rectangle roi)
        {
            CameraMatrix = cameraMatrix;
            DistCoeffs = distCoeffs;
            MapX = mapX;
            MapY = mapY;
            TForm = tForm;
            FrameSizePx = frameSizePx;
            FrameSizeMm = frameSizeMm;
            Roi = roi;

            IsCalibrationValid = !CameraMatrix.IsEmpty && !DistCoeffs.IsEmpty && !MapX.IsEmpty && !MapY.IsEmpty;
            IsWarpValid = !TForm.IsEmpty;
        }

        // Конструктор принимает вычисленные параметры
        public CalibrationSettings(Mat cameraMatrix, Mat distCoeffs, Mat mapX, Mat mapY, Mat tForm, Size frameSizePx,
            Size boardSize, float squareSizeMm, Rectangle roi)
        {
            CameraMatrix = cameraMatrix?.Clone() ?? new Mat();
            DistCoeffs = distCoeffs?.Clone() ?? new Mat();
            MapX = mapX?.Clone() ?? new Mat();
            MapY = mapY?.Clone() ?? new Mat();
            TForm = tForm?.Clone() ?? new Mat();
            FrameSizePx = frameSizePx; // Устанавливаем как есть

            FrameSizeMm = new SizeF(
                (boardSize.Width - 1) * squareSizeMm,
                (boardSize.Height - 1) * squareSizeMm
            );

            Roi = roi;

            IsCalibrationValid = !CameraMatrix.IsEmpty && !DistCoeffs.IsEmpty && !MapX.IsEmpty && !MapY.IsEmpty;
            IsWarpValid = !TForm.IsEmpty;
        }

        // Метод для применения кадра к настройкам
        public Mat ApplyToFrame(Mat frame)
        {
            if (frame == null || frame.IsEmpty)
            {
                return null;
            }

            Mat resultFrame = null;

            if (IsCalibrationValid)
            {
                //  Применяем Remap (исправление искажений)
                Mat undistortedFrame = new Mat();
                CvInvoke.Remap(frame, undistortedFrame, MapX, MapY, Inter.Linear);
                // Обрезаем по ROI
                var croppedUndistortedFrame = new Mat(undistortedFrame, Roi);
                if (IsWarpValid)
                {
                    // Применяем WarpPerspective
                    resultFrame = new Mat();
                    CvInvoke.WarpPerspective(croppedUndistortedFrame, resultFrame, TForm, FrameSizePx);
                    croppedUndistortedFrame.Dispose();
                    undistortedFrame.Dispose();
                }
                else
                {
                    resultFrame = croppedUndistortedFrame;
                    undistortedFrame.Dispose();
                }
            }
            else
            {
                resultFrame = frame.Clone();
            }

            return resultFrame;
        }

        public Mat GetRoiMask(string roiMaskPath)
        {
            var roiMask = CvInvoke.Imread(roiMaskPath);
            return roiMask;
        }

        // Метод для освобождения ресурсов
        public void Dispose()
        {
            CameraMatrix?.Dispose();
            DistCoeffs?.Dispose();
            MapX?.Dispose();
            MapY?.Dispose();
            TForm?.Dispose();
        }
    }
}
