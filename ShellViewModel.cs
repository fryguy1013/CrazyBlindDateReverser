using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CrazyBlindDateReverser
{
    [Export(typeof(IShell))]
    public class ShellViewModel : IShell
    {
        public ObservableCollection<WriteableBitmap> ImagePieces { get; set; }

        public ShellViewModel()
        {
            ImagePieces = new ObservableCollection<WriteableBitmap>();

            foreach (var file in Directory.GetFiles(@"c:\users\kevin\desktop\", "*.jpg"))
                SolveImage(file);
            //SolveImage(@"c:\users\kevin\desktop\85b3f898933780a9ee9f.jpg");
        }

        private void SolveImage(string baseFileName)
        {
            var imagePieces = new List<WriteableBitmap>();

            var bmpImg = new BitmapImage(new Uri(baseFileName, UriKind.Absolute));
            var baseImage = BitmapFactory.ConvertToPbgra32Format(bmpImg);

            int pieceHeight = (int)(baseImage.Height / 4);
            int pieceWidth = (int)(baseImage.Width / 4);

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var bmp = BitmapFactory.New(pieceHeight, pieceWidth);
                    bmp.Blit(new Rect(0, 0, pieceWidth, pieceHeight), baseImage, new Rect(x * pieceWidth, y * pieceHeight, pieceWidth, pieceHeight));
                    imagePieces.Add(bmp);
                }
            }

            var leftRightScores = new Dictionary<Tuple<int, int>, double>();
            var upDownScores = new Dictionary<Tuple<int, int>, double>();

            foreach (var a in Enumerable.Range(0, imagePieces.Count))
            {
                foreach (var b in Enumerable.Range(0, imagePieces.Count))
                {
                    var aImg = imagePieces[a];
                    var bImg = imagePieces[b];

                    double leftRightScore = Enumerable.Range(0, pieceHeight)
                                                      .Average(i => ColorDiff(aImg.GetPixel(pieceWidth - 1, i), bImg.GetPixel(0, i)));
                    //if (leftRightScore < 0.00001)
                    //    leftRightScore = 1;
                    leftRightScores[new Tuple<int, int>(a, b)] = leftRightScore;

                    double upDownScore = Enumerable.Range(0, pieceHeight)
                                                   .Average(i => ColorDiff(aImg.GetPixel(i, pieceHeight - 1), bImg.GetPixel(i, 0)));
                    //if (upDownScore < 0.00001)
                    //    upDownScore = 1;
                    upDownScores[new Tuple<int, int>(a, b)] = upDownScore;
                }
            }

            //var fullMap = new WriteableBitmap[9,9];
            //var remaining = imagePieces.ToList();
            //fullMap[4, 4] = remaining.First();
            //remaining.Remove(fullMap[4, 4]);

            //int ctr = 0;
            //while (remaining.Any())
            //{
            //    if (ctr++ == 2)
            //        break;

            //    var best = new
            //    {
            //        Bmp = (WriteableBitmap)null,
            //        X = -1,
            //        Y = -1,
            //        Score = Double.MaxValue
            //    };

            //    foreach (var item in remaining)
            //    {
            //        for (int x = 0; x < 9; x++)
            //        {
            //            for (int y = 0; y < 9; y++)
            //            {
            //                if (fullMap[x, y] != null)
            //                    continue;

            //                double score = 0;
            //                // check below
            //                if (y < 8 && fullMap[x, y + 1] != null)
            //                {
            //                    score += upDownScores[new Tuple<WriteableBitmap, WriteableBitmap>(item, fullMap[x, y + 1])];
            //                }
            //                else
            //                {
            //                    score += 10;
            //                }

            //                // check above
            //                if (y > 0 && fullMap[x, y - 1] != null)
            //                {
            //                    score += upDownScores[new Tuple<WriteableBitmap, WriteableBitmap>(fullMap[x, y - 1], item)];
            //                }
            //                else
            //                {
            //                    score += 10;
            //                }

            //                // check right
            //                if (x < 8 && fullMap[x + 1, y] != null)
            //                {
            //                    score += leftRightScores[new Tuple<WriteableBitmap, WriteableBitmap>(item, fullMap[x + 1, y])];
            //                }
            //                else
            //                {
            //                    score += 10;
            //                }

            //                // check left
            //                if (x > 0 && fullMap[x - 1, y] != null)
            //                {
            //                    score += leftRightScores[new Tuple<WriteableBitmap, WriteableBitmap>(fullMap[x - 1, y], item)];
            //                }
            //                else
            //                {
            //                    score += 10;
            //                }

            //                if (score < best.Score)
            //                {
            //                    best = new
            //                    {
            //                        Bmp = item,
            //                        X = x,
            //                        Y = y,
            //                        Score = score
            //                    };
            //                }
            //            }
            //        }
            //    }

            //    if (best.Bmp == null)
            //        break;

            //    fullMap[best.X, best.Y] = best.Bmp;
            //    remaining.Remove(best.Bmp);
            //}

            

            int[,] fullMap;
            double beta = 0.000001;
            do
            {
                fullMap = new int[4,4]
                {
                    {
                        -1, -1, -1, -1
                    },
                    {
                        -1, -1, -1, -1
                    },
                    {
                        -1, -1, -1, -1
                    },
                    {
                        -1, -1, -1, -1
                    },
                };

                RecursiveSolver(leftRightScores, upDownScores, Enumerable.Range(0, 16).ToList(), fullMap, 0, 0, beta);

                beta *= 1.05;
            }
            while (!FullyDone(fullMap));

            var finalBmp = BitmapFactory.New(fullMap.GetLength(0) * pieceHeight, fullMap.GetLength(1) * pieceWidth);
            for (int x = 0; x < fullMap.GetLength(0); x++)
            {
                for (int y = 0; y < fullMap.GetLength(1); y++)
                {
                    if (fullMap[x, y] != -1)
                    {
                        finalBmp.Blit(new Rect(x * pieceWidth, y * pieceWidth, pieceWidth, pieceHeight), imagePieces[fullMap[x, y]], new Rect(0, 0, pieceWidth, pieceHeight));
                    }
                }
            }

            ImagePieces.Add(baseImage);
            ImagePieces.Add(finalBmp);
        }

        private bool FullyDone(int[,] fullMap)
        {
            for (int x = 0; x < fullMap.GetLength(0); x++)
            {
                for (int y = 0; y < fullMap.GetLength(1); y++)
                {
                    if (fullMap[x, y] == -1)
                        return false;
                }
            }

            return true;
        }

        private double RecursiveSolver(
            Dictionary<Tuple<int, int>, double> leftRightScores,
            Dictionary<Tuple<int, int>, double> upDownScores,
            IList<int> remaining,
            int[,] fullMap,
            int x = 0,
            int y = 0,
            double beta = double.MaxValue)
        {
            var score = Evaluate(leftRightScores, upDownScores, fullMap);

            if (score > beta)
                return score;

            if (!remaining.Any())
                return score;

            double alpha = double.MaxValue;
            int[,] best = null;
            foreach (var next in remaining)
            {
                var mapCopy = (int[,])fullMap.Clone();
                mapCopy[x, y] = next;
                //int nextX = x == 0 && y < 3 ? y + 1 : y == 3 ? 3 : x - 1;
                //int nextY = x == 0 && y < 3 ? 0 : y == 3 ? x : y + 1;
                int nextX = x == 3 ? 0 : x + 1;
                int nextY = x == 3 ? y + 1 : y;

                double s = RecursiveSolver(leftRightScores, upDownScores, remaining.Where(i => i != next).ToList(), mapCopy, nextX, nextY, beta);
                if (s < alpha)
                {
                    alpha = s;
                    best = mapCopy;
                }
            }

            for (int xx=0; xx<4; xx++)
                for (int yy=0; yy<4; yy++)
                    fullMap[xx, yy] = best[xx, yy];

            return alpha;
        }

        private static double Evaluate(Dictionary<Tuple<int, int>, double> leftRightScores, Dictionary<Tuple<int, int>, double> upDownScores, int[,] fullMap)
        {
            double score = 0;
            int ct = 0;
            for (int xx = 0; xx < 4; xx++)
            {
                for (int yy = 0; yy < 4; yy++)
                {
                    if (xx < 3 && fullMap[xx, yy] != -1 && fullMap[xx + 1, yy] != -1)
                    {
                        score = Math.Max(score, leftRightScores[new Tuple<int, int>(fullMap[xx, yy], fullMap[xx + 1, yy])]);
                        ct++;
                    }
                    if (yy < 3 && fullMap[xx, yy] != -1 && fullMap[xx, yy + 1] != -1)
                    {
                        score = Math.Max(score, upDownScores[new Tuple<int, int>(fullMap[xx, yy], fullMap[xx, yy + 1])]);
                        ct++;
                    }
                }
            }

            return ct > 0 ? score / ct : 0;
        }

        private double ColorDiff(Color a, Color b)
        {
            const double factor = 2;
            return Math.Pow(Math.Abs(a.R - b.R) / 256.0, factor) +
                   Math.Pow(Math.Abs(a.G - b.G) / 256.0, factor) +
                   Math.Pow(Math.Abs(a.B - b.B) / 256.0, factor);
        }
    }
}