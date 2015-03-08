﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.TestCommon;

namespace System.Net.Http.Formatting.Parsers
{
    public class MimeMultipartParserTests
    {
        private const string SP = " ";
        private const string LF = "\n";
        private const string CR = "\r";
        private const string CRLF = "\r\n";
        private const string HTAB = "\t";
        private const string DashDash = "--";

        public static TheoryDataSet<string> Boundaries
        {
            get
            {
                return new TheoryDataSet<string>
                {
                    "1",
                    "a",
                    "'()+_--./:=?",
                    "--",
                    "----",
                    "9df4e21f-6e6f-4b08-8023-97283d2afeeb",
                    "-----------------------------7d159c1302d0y0",
                    "--------------------01234567890123456789",
                    "--------------------01234567890123456789--------------------",
                    "--A--B--C--D--E--F--",
                };
            }
        }

        public static TheoryDataSet<string> SingleShortBodies
        {
            get
            {
                return new TheoryDataSet<string>
                {
                    "",
                    "A",
                    "AA",
                };
            }
        }

        public static TheoryDataSet<string[]> MultipleShortBodies
        {
            get
            {
                return new TheoryDataSet<string[]>
                {
                    CreateMultipleShortBodies("", 26),
                    CreateMultipleShortBodies("{0}", 26),
                    CreateMultipleShortBodies("--{0}", 26),
                };
            }
        }

        public static TheoryDataSet<string> SingleLongBodies
        {
            get
            {
                return new TheoryDataSet<string>
                {
                    CreateLongString("1234567890", "A", 128),
                    CreateLongString("1234567890", "--", 128),
                };
            }
        }

        public static TheoryDataSet<string[]> MultipleLongBodies
        {
            get
            {
                string[] result = new string[16];
                for (int count = 0; count < result.Length; count++)
                {
                    string bookend = Char.ConvertFromUtf32(0x41 + count);
                    result[count] = CreateLongString("1234567890", bookend, 16);
                }

                return new TheoryDataSet<string[]>
                {
                    result
                };
            }
        }

        public static TheoryDataSet<string> NearBoundaryBodies
        {
            get
            {
                return new TheoryDataSet<string>
                {
                    "AAA" + LF,
                    "AAA" + CR,
                    "AAA" + CRLF,
                    "AAA" + CRLF + CRLF,
                    "AAA" + CRLF + "-",
                    "AAA" + CRLF + "-" + CR,
                    "AAA" + CRLF + "=" + CRLF,
                    CR + "-" + "AAA",
                    CRLF + "-" + "AAA",
                    CRLF + "--" + "AAA" + CR + "AAA",
                    CRLF,
                    "AAA",
                    "AAA" + CRLF,
                    CRLF + CRLF,
                    CRLF + CRLF + CRLF,
                    "AAA" + "--" + "AAA",
                    CRLF + "AAA" + "--" + "AAA" + "--",
                    CRLF + "--" + "AAA" + CRLF, 
                    CRLF + "--" + "AAA" + CRLF + CRLF, 
                    CRLF + "--" + "AAA" + "--" + CRLF, 
                    CRLF + "--" + "AAA" + "--" + CRLF + CRLF,
                    "--úN$(Os#»Í(Bt$(Dqf(CS'.Â‚æ0j",
                    "--123456",
                    "123--456",
                    "123456--"
                };
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries")]
        public void MimeMultipartParserConstructorTest(string boundary)
        {
            MimeMultipartParser parser = new MimeMultipartParser(boundary, ParserData.MinMessageSize);

            Assert.ThrowsArgumentGreaterThanOrEqualTo(() => new MimeMultipartParser("-", ParserData.MinMessageSize - 1),
                "maxMessageSize", ParserData.MinMessageSize.ToString(), ParserData.MinMessageSize - 1);

            foreach (string empty in TestData.EmptyStrings)
            {
                Assert.ThrowsArgument(() => { new MimeMultipartParser(empty, ParserData.MinMessageSize); }, "boundary", allowDerivedExceptions: true);
            }

            Assert.ThrowsArgument(() => { new MimeMultipartParser("trailingspace ", ParserData.MinMessageSize); }, "boundary");

            Assert.ThrowsArgumentNull(() => { new MimeMultipartParser(null, ParserData.MinMessageSize); }, "boundary");
        }

        [Fact]
        public void MimeMultipartParser_ThrowsOnTooBigBoundary()
        {
            string maxLegalBoundary = new string('a', 246);
            MimeMultipartParser parser = new MimeMultipartParser(maxLegalBoundary, ParserData.MinMessageSize);

            string minIllegalBoundary = new string('a', 247);
            Assert.ThrowsArgumentLessThanOrEqualTo(() => new MimeMultipartParser(minIllegalBoundary, ParserData.MinMessageSize),
                "boundary", "246", "247");
        }

        [Fact]
        public void MultipartParserNullBuffer()
        {
            MimeMultipartParser parser = CreateMimeMultipartParser(128, "-");

            int bytesConsumed = 0;
            ArraySegment<byte> out1;
            ArraySegment<byte> out2;
            bool isFinal;
            Assert.ThrowsArgumentNull(() => { parser.ParseBuffer(null, 0, ref bytesConsumed, out out1, out out2, out isFinal); }, "buffer");
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries")]
        public void MultipartParserEmptyBuffer(string boundary)
        {
            byte[] data = CreateBuffer(boundary);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);


                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(2, bodyParts.Count);
                Assert.Equal(0, bodyParts[0].Length);
                Assert.Equal(0, bodyParts[1].Length);
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries", typeof(MimeMultipartParserTests), "SingleShortBodies")]
        public void MultipartParserSingleShortBodyPart(string boundary, string singleShortBody)
        {
            byte[] data = CreateBuffer(boundary, singleShortBody);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(2, bodyParts.Count);
                Assert.Equal(0, bodyParts[0].Length);
                Assert.Equal(singleShortBody, bodyParts[1]);
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries", typeof(MimeMultipartParserTests), "MultipleShortBodies")]
        public void MultipartParserMultipleShortBodyParts(string boundary, string[] multipleShortBodies)
        {
            byte[] data = CreateBuffer(boundary, multipleShortBodies);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(multipleShortBodies.Length + 1, bodyParts.Count);
                Assert.Equal(0, bodyParts[0].Length);

                for (var check = 0; check < multipleShortBodies.Length; check++)
                {
                    Assert.Equal(multipleShortBodies[check], bodyParts[check + 1]);
                }
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries", typeof(MimeMultipartParserTests), "MultipleShortBodies")]
        public void MultipartParserMultipleShortBodyPartsWithLws(string boundary, string[] multipleShortBodies)
        {
            byte[] data = CreateBuffer(boundary, true, multipleShortBodies);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(multipleShortBodies.Length + 1, bodyParts.Count);
                Assert.Equal(0, bodyParts[0].Length);

                for (var check = 0; check < multipleShortBodies.Length; check++)
                {
                    Assert.Equal(multipleShortBodies[check], bodyParts[check + 1]);
                }
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries", typeof(MimeMultipartParserTests), "SingleLongBodies")]
        public void MultipartParserSingleLongBodyPart(string boundary, string singleLongBody)
        {
            byte[] data = CreateBuffer(boundary, singleLongBody);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(2, bodyParts.Count);
                Assert.Equal(0, bodyParts[0].Length);

                Assert.Equal(singleLongBody.Length, bodyParts[1].Length);
                Assert.Equal(singleLongBody, bodyParts[1]);
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries", typeof(MimeMultipartParserTests), "MultipleLongBodies")]
        public void MultipartParserMultipleLongBodyParts(string boundary, string[] multipleLongBodies)
        {
            byte[] data = CreateBuffer(boundary, multipleLongBodies);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(multipleLongBodies.Length + 1, bodyParts.Count);
                Assert.Equal(0, bodyParts[0].Length);

                for (var check = 0; check < multipleLongBodies.Length; check++)
                {
                    Assert.Equal(multipleLongBodies[check], bodyParts[check + 1]);
                }
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries", typeof(MimeMultipartParserTests), "NearBoundaryBodies")]
        public void MultipartParserNearMatches(string boundary, string nearBoundaryBody)
        {
            byte[] data = CreateBuffer(boundary, nearBoundaryBody);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(2, bodyParts.Count);
                Assert.Equal(0, bodyParts[0].Length);
                Assert.Equal(nearBoundaryBody, bodyParts[1]);
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries")]
        public void MultipartParserNesting(string boundary)
        {
            for (var nesting = 0; nesting < 16; nesting++)
            {
                string nested = CreateNestedBuffer(nesting);

                byte[] data = CreateBuffer(boundary, nested);

                for (var cnt = 1; cnt <= data.Length; cnt++)
                {
                    MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                    int totalBytesConsumed;
                    List<string> bodyParts;
                    MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                    Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                    Assert.Equal(data.Length, totalBytesConsumed);

                    Assert.Equal(2, bodyParts.Count);
                    Assert.Equal(0, bodyParts[0].Length);
                    Assert.Equal(nested.Length, bodyParts[1].Length);
                }
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries")]
        public void MimeMultipartParserTestDataTooBig(string boundary)
        {
            byte[] data = CreateBuffer(boundary);

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(ParserData.MinMessageSize, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.DataTooBig, state);
                Assert.Equal(ParserData.MinMessageSize, totalBytesConsumed);
            }
        }

        [Theory]
        [TestDataSet(typeof(MimeMultipartParserTests), "Boundaries")]
        public void MimeMultipartParserTestMultipartContent(string boundary)
        {
            MultipartContent content = new MultipartContent("mixed", boundary);
            content.Add(new StringContent("A"));
            content.Add(new StringContent("B"));
            content.Add(new StringContent("C"));

            MemoryStream memStream = new MemoryStream();
            content.CopyToAsync(memStream).Wait();
            memStream.Position = 0;
            byte[] data = memStream.ToArray();

            for (var cnt = 1; cnt <= data.Length; cnt++)
            {
                MimeMultipartParser parser = CreateMimeMultipartParser(data.Length, boundary);

                int totalBytesConsumed;
                List<string> bodyParts;
                MimeMultipartParser.State state = ParseBufferInSteps(parser, data, cnt, out bodyParts, out totalBytesConsumed);
                Assert.Equal(MimeMultipartParser.State.BodyPartCompleted, state);
                Assert.Equal(data.Length, totalBytesConsumed);

                Assert.Equal(4, bodyParts.Count);
                Assert.Empty(bodyParts[0]);

                Assert.True(bodyParts[1].EndsWith("A"));
                Assert.True(bodyParts[2].EndsWith("B"));
                Assert.True(bodyParts[3].EndsWith("C"));
            }
        }

        private static MimeMultipartParser CreateMimeMultipartParser(int maximumHeaderLength, string boundary)
        {
            return new MimeMultipartParser(boundary, maximumHeaderLength);
        }

        internal static byte[] CreateBuffer(string boundary, params string[] bodyparts)
        {
            return CreateBuffer(boundary, false, bodyparts);
        }

        internal static string CreateNestedBuffer(int count)
        {
            StringBuilder buffer = new StringBuilder("content");

            for (var cnt = 0; cnt < count; cnt++)
            {
                byte[] nested = CreateBuffer("N" + cnt.ToString(), buffer.ToString());
                var message = Encoding.UTF8.GetString(nested);
                buffer.Length = 0;
                buffer.AppendLine(message);
            }

            return buffer.ToString();
        }

        private static byte[] CreateBuffer(string boundary, bool withLws, params string[] bodyparts)
        {
            string lws = String.Empty;
            if (withLws)
            {
                lws = SP + SP + HTAB + SP;
            }

            StringBuilder message = new StringBuilder();
            message.Append(DashDash + boundary + lws + CRLF);
            for (var cnt = 0; cnt < bodyparts.Length; cnt++)
            {
                message.Append(bodyparts[cnt]);
                if (cnt < bodyparts.Length - 1)
                {
                    message.Append(CRLF + DashDash + boundary + lws + CRLF);
                }
            }

            // Note: We rely on a final CRLF even though it is not required by the BNF existing application do send it
            message.Append(CRLF + DashDash + boundary + DashDash + lws + CRLF);
            return Encoding.UTF8.GetBytes(message.ToString());
        }

        private static MimeMultipartParser.State ParseBufferInSteps(MimeMultipartParser parser, byte[] buffer, int readsize, out List<string> bodyParts, out int totalBytesConsumed)
        {
            MimeMultipartParser.State state = MimeMultipartParser.State.Invalid;
            totalBytesConsumed = 0;
            bodyParts = new List<string>();
            bool isFinal = false;
            byte[] currentBodyPart = new byte[32 * 1024];
            int currentBodyLength = 0;

            while (totalBytesConsumed <= buffer.Length)
            {
                int size = Math.Min(buffer.Length - totalBytesConsumed, readsize);
                byte[] parseBuffer = new byte[size];
                Buffer.BlockCopy(buffer, totalBytesConsumed, parseBuffer, 0, size);

                int bytesConsumed = 0;
                ArraySegment<byte> out1;
                ArraySegment<byte> out2;
                state = parser.ParseBuffer(parseBuffer, parseBuffer.Length, ref bytesConsumed, out out1, out out2, out isFinal);
                totalBytesConsumed += bytesConsumed;

                Buffer.BlockCopy(out1.Array, out1.Offset, currentBodyPart, currentBodyLength, out1.Count);
                currentBodyLength += out1.Count;

                Buffer.BlockCopy(out2.Array, out2.Offset, currentBodyPart, currentBodyLength, out2.Count);
                currentBodyLength += out2.Count;

                if (state == MimeMultipartParser.State.BodyPartCompleted)
                {
                    var bPart = new byte[currentBodyLength];
                    Buffer.BlockCopy(currentBodyPart, 0, bPart, 0, currentBodyLength);
                    bodyParts.Add(Encoding.UTF8.GetString(bPart));
                    currentBodyLength = 0;
                    if (isFinal)
                    {
                        break;
                    }
                }
                else if (state != MimeMultipartParser.State.NeedMoreData)
                {
                    return state;
                }
            }

            Assert.True(isFinal);
            return state;
        }

        private static string[] CreateMultipleShortBodies(string format, int iterations)
        {
            string[] result = new string[iterations];
            for (int count = 0; count < iterations; count++)
            {
                result[count] = string.Format(format, Char.ConvertFromUtf32(0x41 + count));
            }
            return result;
        }

        private static string CreateLongString(string msg, string bookend, int iterations)
        {
            StringBuilder longBody = new StringBuilder();
            if (!String.IsNullOrEmpty(bookend))
            {
                longBody.Append(bookend);
            }

            for (int cnt = 0; cnt < iterations; cnt++)
            {
                longBody.Append(msg);
            }

            if (!String.IsNullOrEmpty(bookend))
            {
                longBody.Append(bookend);
            }

            return longBody.ToString();
        }
    }
}