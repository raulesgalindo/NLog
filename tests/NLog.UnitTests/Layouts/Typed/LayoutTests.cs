using NLog.Layouts;
using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace NLog.UnitTests.Layouts.Typed
{
    public class LayoutTests : NLogTestBase
    {
        [Theory]
        [InlineData(100)]
        [InlineData(100d)]
        [InlineData("100")]
        [InlineData(" 100 ")]
        public void TypedIntLayoutDynamicTest(object value)
        {
            // Arrange
            var layout = CreateLayoutRenderedFromProperty<int>();
            var logEventInfo = CreateLogEventInfoWithValue(value);

            // Act
            var result = layout.RenderableToValue(logEventInfo);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void TypedNullableIntToIntLayoutDynamicTest()
        {
            // Arrange
            var layout = CreateLayoutRenderedFromProperty<int>();
            int? value = 100;
            var logEventInfo = CreateLogEventInfoWithValue(value);

            // Act
            var result = layout.RenderableToValue(logEventInfo);

            // Assert
            Assert.Equal(100, result);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(100d)]
        [InlineData("100")]
        [InlineData(" 100 ")]
        public void TypedNullableIntLayoutDynamicTest(object value)
        {
            // Arrange
            var layout = CreateLayoutRenderedFromProperty<int?>();
            var logEventInfo = CreateLogEventInfoWithValue(value);

            // Act
            var result = layout.RenderableToValue(logEventInfo);

            // Assert
            Assert.Equal(100, result);
        }

        [Theory]
        [InlineData(100.5)]
        [InlineData("100.5", "EN-us")]
        [InlineData("  100.5  ", "EN-us")]
        public void TypedDecimalLayoutDynamicTest(object value, string culture = null)
        {
            // Arrange
            var oldCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                }

                var layout = CreateLayoutRenderedFromProperty<decimal>();
                var logEventInfo = CreateLogEventInfoWithValue(value);

                // Act
                var result = layout.RenderableToValue(logEventInfo);

                // Assert
                decimal expected = 100.5m;
                Assert.Equal(expected, result);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData("true")]
        [InlineData(" true ")]
        public void TypedBoolLayoutDynamicTest(object value)
        {
            // Arrange
            var layout = CreateLayoutRenderedFromProperty<bool>();
            var logEventInfo = CreateLogEventInfoWithValue(value);

            // Act
            var result = layout.RenderableToValue(logEventInfo);

            // Assert
            Assert.True(result);
        }


        [Fact]
        public void ComplexTypeTest()
        {
            // Arrange
            var value = new TestObject { Value = "123" };
            var layout = CreateLayoutRenderedFromProperty<TestObject>();
            var logEventInfo = CreateLogEventInfoWithValue(value);

            // Act
            var result = layout.RenderableToValue(logEventInfo);

            // Assert
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WrongValueErrorTest(bool throwsException)
        {
            // Arrange
            LogManager.ThrowExceptions = throwsException; //will be reset by NLogTestBase for every test
            var value = "12312aa3";
            var layout = CreateLayoutRenderedFromProperty<int>();
            var logEventInfo = CreateLogEventInfoWithValue(value);

            // Act
            Action action = () => layout.RenderableToValue(logEventInfo);

            // Assert
            if (throwsException)
            {
                Assert.Throws<FormatException>(action);
            }
            else
            {
                action();
            }
        }

        private class TestObject
        {
            public string Value { get; set; }
        }

        private static Layout<T> CreateLayoutRenderedFromProperty<T>()
        {
            var layout = new Layout<T>("${event-properties:value1}");
            return layout;
        }

        private static LogEventInfo CreateLogEventInfoWithValue(object value)
        {
            var logEventInfo = LogEventInfo.Create(LogLevel.Info, "logger1", "message1");
            logEventInfo.Properties.Add("value1", value);
            return logEventInfo;
        }
    }
}