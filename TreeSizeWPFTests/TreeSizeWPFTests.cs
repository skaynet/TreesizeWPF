using Microsoft.VisualStudio.TestTools.UnitTesting;
using TreeSizeWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeSizeWPF.Tests
{
    [TestClass()]
    public class TreeSizeWPFTests
    {
        [TestMethod]
        public void TestConvertBytes_BytesLessThan1KB_ReturnsBytesString()
        {
            // Arrange
            long bytes = 500;
            bool isAddUnits = true;

            // Act
            string result = AnalyzeDirectoryService.ConvertBytes(bytes, isAddUnits);

            // Assert
            Assert.AreEqual("500 Б", result);
        }

        [TestMethod]
        public void TestConvertBytes_BytesInKB_ReturnsKBString()
        {
            // Arrange
            long bytes = 1500;
            bool isAddUnits = true;

            // Act
            string result = AnalyzeDirectoryService.ConvertBytes(bytes, isAddUnits);

            // Assert
            Assert.AreEqual("1.46 КБ", result);
        }

        [TestMethod]
        public void TestConvertBytes_BytesInMB_ReturnsMBString()
        {
            // Arrange
            long bytes = 3500000;
            bool isAddUnits = true;

            // Act
            string result = AnalyzeDirectoryService.ConvertBytes(bytes, isAddUnits);

            // Assert
            Assert.AreEqual("3.34 МБ", result);
        }

        [TestMethod]
        public void TestConvertBytes_BytesInGB_ReturnsGBString()
        {
            // Arrange
            long bytes = 10000000000;
            bool isAddUnits = true;

            // Act
            string result = AnalyzeDirectoryService.ConvertBytes(bytes, isAddUnits);

            // Assert
            Assert.AreEqual("9.31 ГБ", result);
        }

        [TestMethod]
        public void TestConvertBytes_BytesInTB_ReturnsTBString()
        {
            // Arrange
            long bytes = 5000000000000;
            bool isAddUnits = true;

            // Act
            string result = AnalyzeDirectoryService.ConvertBytes(bytes, isAddUnits);

            // Assert
            Assert.AreEqual("4.55 ТБ", result);
        }

        [TestMethod]
        public void TestConvertBytes_NoUnits_ReturnsNumberString()
        {
            // Arrange
            long bytes = 1024;
            bool isAddUnits = false;

            // Act
            string result = AnalyzeDirectoryService.ConvertBytes(bytes, isAddUnits);

            // Assert
            Assert.AreEqual("1", result);
        }
    }
}