﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Essentials;
using xamarin_lib_harpia.Models.Entities;
using System.Text;
using ZXing.QrCode.Internal;
using Xamarin.Forms.Xaml;

namespace xamarin_lib_harpia.Utils
{
    public class CommandUtils
    {
        public static byte ESC = 0x1B;// Escape
        public static byte FS = 0x1C;// Text delimiter
        public static byte GS = 0x1D;// Group separator
        public static byte DLE = 0x10;// data link escape
        public static byte EOT = 0x04;// End of transmission
        public static byte ENQ = 0x05;// Enquiry character
        public static byte SP = 0x20;// Spaces
        public static byte HT = 0x09;// Horizontal list
        public static byte LF = 0x0A;//Print and wrap (horizontal orientation)
        public static byte CR = 0x0D;// Home key
        public static byte FF = 0x0C;// Carriage control (print and return to the standard mode (in page mode))
        public static byte CAN = 0x18;// Canceled (cancel print data in page mode)

        private static byte[] GetBytesFromDecString(string decstring)
        {
            if (decstring == null || decstring.Equals("")) return null;
            decstring = decstring.Replace(" ", "");
            int size = decstring.Length / 2;
            char[] decarray = decstring.ToCharArray();
            byte[] rv = new byte[size];
            for (int i = 0; i < size; i++)
            {
                int pos = i * 2;
                rv[i] = (byte)(Convert.ToByte(decarray[pos]) * 10 + Convert.ToByte(decarray[pos + 1]));
            }
            return rv;
        }

        public static byte[] GetBarcodeBytes(Barcode barcode)
        {
            int position = barcode.HRIPosition == "Acima do QRCode" ? 1 :
                barcode.HRIPosition == "Abaixo do QRCode" ? 2 :
                barcode.HRIPosition == "Acima e abaixo do QRCode" ? 3 :
                0;
            byte[] dimensions = new byte[]{0x1D,0x66,0x01,0x1D,0x48,
                    (byte)position,
                    0x1D,0x77,
                    (byte)barcode.Width,
                    0x1D,0x68,
                    (byte)barcode.Height,
                    0x0A
            };
            byte[] barcodeByteArray;

            if (barcode.Model.ID == 10)
                barcodeByteArray = GetBytesFromDecString(barcode.Content);
            else
                barcodeByteArray = TextToByte(barcode.Content);

            byte[] model;
            if (barcode.Model.ID > 7)
                model = new byte[] { 0x1D, 0x6B, 0x49, (byte)(barcodeByteArray.Length + 2), 0x7B, (byte)(0x41 + barcode.Model.ID - 8) };
            else
                model = new byte[] { 0x1D, 0x6B, (byte)(barcode.Model.ID + 0x41), (byte)barcodeByteArray.Length };

            var stream = new List<byte>();
            stream.AddRange(TextToByte("Barcode\n"));
            stream.AddRange(TextToByte("--------------------------------\n"));
            stream.AddRange(dimensions); // Setting barcode dimensions (width, height, alingment)
            stream.AddRange(model); // Setting the barcode model
            stream.AddRange(barcodeByteArray); // Setting the barcode content
            if (barcode.CutPaper) stream.AddRange(CutPaper());
            return stream.ToArray();
        }
        public static byte[] GetQrcodeBytes(QRcode qrcode)
        {
            //modulesize
            byte[] modulesize = new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)qrcode.ImpSize};

            //errorlevel
            byte[] errorlevel = new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, (byte)(48 + (int)qrcode.Correction) };

            // code 
            var stream_code = new List<byte>();
            byte[] d = TextToByte(qrcode.Content);
            int len = d.Length + 3;
            byte[] c = new byte[] { 0x1D, 0x28, 0x6B, (byte)len, (byte)(len >> 8), 0x31, 0x50, 0x30 };
            stream_code.AddRange(c);
            for (int i = 0; i < d.Length && i < len; i++)
            {
                stream_code.Add(d[i]);
            }
            var code = stream_code.ToArray();


            if (qrcode.ImpQuant == 0)
            {
                var stream = new List<byte>();
                stream.AddRange(TextToByte("QrCode\n"));
                stream.AddRange(TextToByte("--------------------------------\n"));
                stream.AddRange(modulesize);
                stream.AddRange(errorlevel);
                stream.AddRange(code);
                if (qrcode.Alignment == AlignmentEnum.CENTER)
                {
                    stream.AddRange(AlignCenter());
                }
                else if (qrcode.Alignment == AlignmentEnum.RIGHT)
                {
                    stream.AddRange(AlignRight());
                }
                else
                {
                    stream.AddRange(AlignLeft());
                }
                stream.AddRange(getBytesForPrintQRCode(true));
                if (qrcode.CutPaper) stream.AddRange(CutPaper());
                return stream.ToArray();
            }
            else
            {
                byte[] double_qr = new byte[] { 0x1B, 0x5C, 0x18, 0x00 };
                var stream = new List<byte>();
                stream.AddRange(TextToByte("QrCode\n"));
                stream.AddRange(TextToByte("--------------------------------\n"));
                stream.AddRange(modulesize);
                stream.AddRange(errorlevel);
                stream.AddRange(code);
                if (qrcode.Alignment == AlignmentEnum.CENTER)
                {
                    stream.AddRange(AlignCenter());
                } 
                else if (qrcode.Alignment == AlignmentEnum.RIGHT)
                {
                    stream.AddRange(AlignRight());
                }
                else
                {
                    stream.AddRange(AlignLeft());
                }
                stream.AddRange(getBytesForPrintQRCode(false));
                stream.AddRange(code);
                stream.AddRange(double_qr);
                stream.AddRange(getBytesForPrintQRCode(true));
                if (qrcode.CutPaper) stream.AddRange(CutPaper());
                return stream.ToArray();
            }

        }

        public static byte[] getBytesForPrintQRCode(bool single)
        {
            byte[] bytesforprint;
            if (single)
            {
                bytesforprint = new byte[9];
                bytesforprint[8] = 0x0A;
            }
            else
            {
                bytesforprint = new byte[8];
            }
            bytesforprint = new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 };

            return bytesforprint;
        }

        public static byte[] GetTextBytes(Text text)
        {
            var stream = new List<byte>();

            if (text.IsBold) stream.AddRange(BoldOn());
            else stream.AddRange(BoldOff());

            if (text.IsUnderline) stream.AddRange(UnderlineWithOneDotWidthOn());
            else stream.AddRange(UnderlineOff());

            if (text.Record < 17)
            {
                stream.AddRange(SingleByteOn());
                stream.AddRange(SetCodeSystemSingle(CodeParse(text.Record)));
            } 
            else
            {
                stream.AddRange(SingleByteOff());
                stream.AddRange(SetCodeSystem(CodeParse(text.Record)));
            }

            stream.AddRange(SetFontSize(text.TextSize));
            stream.AddRange(TextToByteEncoding(text.Content, text.Encoding));
            stream.AddRange(NextLine(3));

            return stream.ToArray();
        } 

        public static byte[] BoldOn()
        {
            byte[] result = new byte[] { ESC, 69, 0xf };
            return result;
        }

        public static byte[] BoldOff()
        {
            byte[] result = new byte[] { ESC, 69, 0 };
            return result;
        }

        public static byte[] UnderlineWithOneDotWidthOn()
        {
            byte[] result = new byte[] { ESC, 45, 1 };
            return result;
        }

        public static byte[] UnderlineOff()
        {
            byte[] result = new byte[] { ESC, 45, 0 };
            return result;
        }

        public static byte[] SingleByteOn()
        {
            byte[] result = new byte[] { FS, 0x2E };
            return result;
        }

        public static byte[] SingleByteOff()
        {
            byte[] result = new byte[] { FS, 0x26 };
            return result;
        }

        public static byte CodeParse(int value)
        {
            byte res = 0x00;
            switch(value)
            {
                case 0:
                    res = 0x00;
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                    res = (byte)(value + 1);
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                    res = (byte)(value + 8);
                    break;
                case 12:
                    res = 21;
                    break;
                case 13:
                    res = 33;
                    break;
                case 14:
                    res = 34;
                    break;
                case 15:
                    res = 36;
                    break;
                case 16:
                    res = 37;
                    break;
                case 17:
                case 18:
                case 19:
                    res = (byte)(value - 17);
                    break;
                case 20:
                    res = (byte)0xff;
                    break;
                default:
                    break;
            }
            return (byte)res;
        }

        public static byte[] SetCodeSystemSingle(byte charset)
        {
            byte[] result = new byte[] { ESC, 0x74, charset };
            return result;
        }

        public static byte[] SetCodeSystem(byte charset)
        {
            byte[] result = new byte[] { FS, 0x43, charset };
            return result;
        }

        public static byte[] NextLine(int lineNum)
        {
            byte[] result = new byte[lineNum];
            for (int i = 0; i < lineNum; i++)
            {
                result[i] = LF;
            }
            return result;
        }

        public static byte[] SetFontSize(int fontSize)
        {
            byte[] result = new byte[] { 0x1D, 0x21, (byte)(fontSize - 12) };
            return result;
        }

        public static byte[] AlignLeft()
        {
            return new byte[] { ESC, 97, 0 };
        }

        public static byte[] AlignCenter()
        {
            return new byte[] { ESC, 97, 1 };
        }

        public static byte[] AlignRight()
        {
            return new byte[] { ESC, 97, 2 };
        }

        public static byte[] CutPaper()
        {
            return new byte[] { 0x1d, 0x56, 0x01 };
        }

        public static byte[] TextToByte(string content)
        {
            return System.Text.Encoding.ASCII.GetBytes(content);
        }

        public static byte[] TextToByteEncoding(string content, string encoding)
        {
            if (encoding.Equals("utf-8")) return System.Text.Encoding.UTF8.GetBytes(content);
            return System.Text.Encoding.GetEncoding(encoding).GetBytes(content);
        }
    }
}
