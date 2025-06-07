using System.Text;

namespace iLearn.Helpers
{
    public static class DesEncryption
    {
        private static byte[][] GenerateKeys(byte[] keyByte)
        {
            var key = new byte[56];
            var keys = new byte[16][];
            for (int i = 0; i < 16; i++)
                keys[i] = new byte[48];

            var loop = new int[] { 1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1 };

            for (int i = 0; i < 7; i++)
            {
                for (int j = 0, k = 7; j < 8; j++, k--)
                {
                    key[i * 8 + j] = keyByte[8 * k + i];
                }
            }

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < loop[i]; j++)
                {
                    var temp0 = key[0];
                    var temp28 = key[28];

                    for (int k = 0; k < 27; k++)
                    {
                        key[k] = key[k + 1];
                        key[28 + k] = key[29 + k];
                    }
                    key[27] = temp0;
                    key[55] = temp28;
                }

                var tempKey = new byte[]
                {
                    key[13], key[16], key[10], key[23], key[0], key[4], key[2], key[27],
                    key[14], key[5], key[20], key[9], key[22], key[18], key[11], key[3],
                    key[25], key[7], key[15], key[6], key[26], key[19], key[12], key[1],
                    key[40], key[51], key[30], key[36], key[46], key[54], key[29], key[39],
                    key[50], key[44], key[32], key[47], key[43], key[48], key[38], key[55],
                    key[33], key[52], key[45], key[41], key[49], key[35], key[28], key[31]
                };
                Array.Copy(tempKey, keys[i], 48);
            }
            return keys;
        }

        private static string GetBoxBinary(int i)
        {
            return Convert.ToString(i, 2).PadLeft(4, '0');
        }

        private static byte[] InitPermute(byte[] originalData)
        {
            var ipByte = new byte[64];
            var pairs = new (int, int)[] { (1, 0), (3, 2), (5, 4), (7, 6) };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 7, k = 0; j >= 0; j--, k++)
                {
                    ipByte[i * 8 + k] = originalData[j * 8 + pairs[i].Item1];
                    ipByte[i * 8 + k + 32] = originalData[j * 8 + pairs[i].Item2];
                }
            }
            return ipByte;
        }

        private static byte[] ExpandPermute(byte[] rightData)
        {
            var epByte = new byte[48];
            for (int i = 0; i < 8; i++)
            {
                epByte[i * 6 + 0] = rightData[i == 0 ? 31 : i * 4 - 1];
                epByte[i * 6 + 1] = rightData[i * 4];
                epByte[i * 6 + 2] = rightData[i * 4 + 1];
                epByte[i * 6 + 3] = rightData[i * 4 + 2];
                epByte[i * 6 + 4] = rightData[i * 4 + 3];
                epByte[i * 6 + 5] = rightData[i == 7 ? 0 : i * 4 + 4];
            }
            return epByte;
        }

        private static byte[] Xor(byte[] byteOne, byte[] byteTwo)
        {
            var result = new byte[byteOne.Length];
            for (int i = 0; i < byteOne.Length; i++)
            {
                result[i] = (byte)(byteOne[i] ^ byteTwo[i]);
            }
            return result;
        }

        private static byte[] SBoxPermute(byte[] expandByte)
        {
            var sBoxByte = new byte[32];
            var sBoxes = new int[][][]
            {
                new int[][]
                {
                    new int[] { 14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7 },
                    new int[] { 0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8 },
                    new int[] { 4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0 },
                    new int[] { 15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13 }
                },
                new int[][]
                {
                    new int[] { 15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10 },
                    new int[] { 3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5 },
                    new int[] { 0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15 },
                    new int[] { 13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9 }
                },
                new int[][]
                {
                    new int[] { 10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8 },
                    new int[] { 13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1 },
                    new int[] { 13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7 },
                    new int[] { 1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12 }
                },
                new int[][]
                {
                    new int[] { 7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15 },
                    new int[] { 13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9 },
                    new int[] { 10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4 },
                    new int[] { 3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14 }
                },
                new int[][]
                {
                    new int[] { 2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9 },
                    new int[] { 14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6 },
                    new int[] { 4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14 },
                    new int[] { 11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3 }
                },
                new int[][]
                {
                    new int[] { 12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11 },
                    new int[] { 10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8 },
                    new int[] { 9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6 },
                    new int[] { 4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13 }
                },
                new int[][]
                {
                    new int[] { 4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1 },
                    new int[] { 13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6 },
                    new int[] { 1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2 },
                    new int[] { 6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12 }
                },
                new int[][]
                {
                    new int[] { 13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7 },
                    new int[] { 1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2 },
                    new int[] { 7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8 },
                    new int[] { 2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11 }
                }
            };

            for (int m = 0; m < 8; m++)
            {
                var i = expandByte[m * 6] * 2 + expandByte[m * 6 + 5];
                var j = expandByte[m * 6 + 1] * 8 + expandByte[m * 6 + 2] * 4 + expandByte[m * 6 + 3] * 2 + expandByte[m * 6 + 4];

                var binaryStr = GetBoxBinary(sBoxes[m][i][j]);
                sBoxByte[m * 4] = (byte)(binaryStr[0] - '0');
                sBoxByte[m * 4 + 1] = (byte)(binaryStr[1] - '0');
                sBoxByte[m * 4 + 2] = (byte)(binaryStr[2] - '0');
                sBoxByte[m * 4 + 3] = (byte)(binaryStr[3] - '0');
            }
            return sBoxByte;
        }

        private static byte[] PPermute(byte[] sBoxByte)
        {
            return new byte[]
            {
                sBoxByte[15], sBoxByte[6], sBoxByte[19], sBoxByte[20], sBoxByte[28], sBoxByte[11], sBoxByte[27], sBoxByte[16],
                sBoxByte[0], sBoxByte[14], sBoxByte[22], sBoxByte[25], sBoxByte[4], sBoxByte[17], sBoxByte[30], sBoxByte[9],
                sBoxByte[1], sBoxByte[7], sBoxByte[23], sBoxByte[13], sBoxByte[31], sBoxByte[26], sBoxByte[2], sBoxByte[8],
                sBoxByte[18], sBoxByte[12], sBoxByte[29], sBoxByte[5], sBoxByte[21], sBoxByte[10], sBoxByte[3], sBoxByte[24]
            };
        }

        private static byte[] FinallyPermute(byte[] endByte)
        {
            return new byte[]
            {
                endByte[39], endByte[7], endByte[47], endByte[15], endByte[55], endByte[23], endByte[63], endByte[31],
                endByte[38], endByte[6], endByte[46], endByte[14], endByte[54], endByte[22], endByte[62], endByte[30],
                endByte[37], endByte[5], endByte[45], endByte[13], endByte[53], endByte[21], endByte[61], endByte[29],
                endByte[36], endByte[4], endByte[44], endByte[12], endByte[52], endByte[20], endByte[60], endByte[28],
                endByte[35], endByte[3], endByte[43], endByte[11], endByte[51], endByte[19], endByte[59], endByte[27],
                endByte[34], endByte[2], endByte[42], endByte[10], endByte[50], endByte[18], endByte[58], endByte[26],
                endByte[33], endByte[1], endByte[41], endByte[9], endByte[49], endByte[17], endByte[57], endByte[25],
                endByte[32], endByte[0], endByte[40], endByte[8], endByte[48], endByte[16], endByte[56], endByte[24]
            };
        }

        private static byte[] StrToBt(string str)
        {
            var bt = new byte[64];
            if (str.Length < 4)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    var k = str[i];
                    for (int j = 0; j < 16; j++)
                    {
                        var pow = 1 << (15 - j);
                        bt[16 * i + j] = (byte)(k / pow % 2);
                    }
                }
                for (int p = str.Length; p < 4; p++)
                {
                    for (int q = 0; q < 16; q++)
                    {
                        bt[16 * p + q] = 0;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    var k = str[i];
                    for (int j = 0; j < 16; j++)
                    {
                        var pow = 1 << (15 - j);
                        bt[16 * i + j] = (byte)(k / pow % 2);
                    }
                }
            }
            return bt;
        }

        private static string Bt64ToHex(byte[] byteData)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                var binaryStr = new StringBuilder();
                for (int j = 0; j < 4; j++)
                {
                    binaryStr.Append(byteData[i * 4 + j]);
                }
                var value = Convert.ToInt32(binaryStr.ToString(), 2);
                sb.Append(value.ToString("X"));
            }
            return sb.ToString();
        }

        private static byte[][] GetKeyBytes(string key)
        {
            var iterator = key.Length / 4;
            var keyBytes = new byte[iterator + 1][];

            for (int i = 0; i < iterator; i++)
            {
                keyBytes[i] = StrToBt(key.Substring(i * 4, 4));
            }

            if (key.Length % 4 > 0)
            {
                keyBytes[iterator] = StrToBt(key.Substring(iterator * 4));
            }

            return keyBytes;
        }

        private static byte[] Enc(byte[] dataByte, byte[] keyByte)
        {
            var keys = GenerateKeys(keyByte);
            var ipByte = InitPermute(dataByte);
            var ipLeft = new byte[32];
            var ipRight = new byte[32];
            var tempLeft = new byte[32];

            Array.Copy(ipByte, 0, ipLeft, 0, 32);
            Array.Copy(ipByte, 32, ipRight, 0, 32);

            for (int i = 0; i < 16; i++)
            {
                Array.Copy(ipLeft, tempLeft, 32);
                Array.Copy(ipRight, ipLeft, 32);

                var tempRight = Xor(PPermute(SBoxPermute(Xor(ExpandPermute(ipRight), keys[i]))), tempLeft);
                Array.Copy(tempRight, ipRight, 32);
            }

            var finalData = new byte[64];
            Array.Copy(ipRight, 0, finalData, 0, 32);
            Array.Copy(ipLeft, 0, finalData, 32, 32);

            return FinallyPermute(finalData);
        }

        public static string StrEnc(string data, string? firstKey, string? secondKey, string? thirdKey)
        {
            var encData = new StringBuilder();
            byte[][]? firstKeyBt = null;
            byte[][]? secondKeyBt = null;
            byte[][]? thirdKeyBt = null;
            int firstLength = 0, secondLength = 0, thirdLength = 0;

            if (!string.IsNullOrEmpty(firstKey))
            {
                firstKeyBt = GetKeyBytes(firstKey);
                firstLength = firstKeyBt.Length;
            }
            if (!string.IsNullOrEmpty(secondKey))
            {
                secondKeyBt = GetKeyBytes(secondKey);
                secondLength = secondKeyBt.Length;
            }
            if (!string.IsNullOrEmpty(thirdKey))
            {
                thirdKeyBt = GetKeyBytes(thirdKey);
                thirdLength = thirdKeyBt.Length;
            }

            if (!string.IsNullOrEmpty(data))
            {
                if (data.Length < 4)
                {
                    var bt = StrToBt(data);
                    var encByte = EncryptWithKeys(bt, firstKeyBt, secondKeyBt, thirdKeyBt, firstLength, secondLength, thirdLength);
                    encData.Append(Bt64ToHex(encByte));
                }
                else
                {
                    var iterator = data.Length / 4;
                    var remainder = data.Length % 4;

                    for (int i = 0; i < iterator; i++)
                    {
                        var tempData = data.Substring(i * 4, 4);
                        var tempByte = StrToBt(tempData);
                        var encByte = EncryptWithKeys(tempByte, firstKeyBt, secondKeyBt, thirdKeyBt, firstLength, secondLength, thirdLength);
                        encData.Append(Bt64ToHex(encByte));
                    }

                    if (remainder > 0)
                    {
                        var remainderData = data.Substring(iterator * 4);
                        var tempByte = StrToBt(remainderData);
                        var encByte = EncryptWithKeys(tempByte, firstKeyBt, secondKeyBt, thirdKeyBt, firstLength, secondLength, thirdLength);
                        encData.Append(Bt64ToHex(encByte));
                    }
                }
            }

            return encData.ToString();
        }

        private static byte[] EncryptWithKeys(byte[] bt, byte[][]? firstKeyBt, byte[][]? secondKeyBt, byte[][]? thirdKeyBt,
            int firstLength, int secondLength, int thirdLength)
        {
            var tempBt = bt;

            if (firstKeyBt != null)
            {
                for (int x = 0; x < firstLength; x++)
                {
                    if (firstKeyBt[x] != null)
                        tempBt = Enc(tempBt, firstKeyBt[x]);
                }
            }

            if (secondKeyBt != null)
            {
                for (int y = 0; y < secondLength; y++)
                {
                    if (secondKeyBt[y] != null)
                        tempBt = Enc(tempBt, secondKeyBt[y]);
                }
            }

            if (thirdKeyBt != null)
            {
                for (int z = 0; z < thirdLength; z++)
                {
                    if (thirdKeyBt[z] != null)
                        tempBt = Enc(tempBt, thirdKeyBt[z]);
                }
            }

            return tempBt;
        }
    }
}
