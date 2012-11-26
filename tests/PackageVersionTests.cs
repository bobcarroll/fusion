using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Fusion.Framework;

namespace tests
{
    [TestClass]
    public class PackageVersionTests
    {
        [TestMethod]
        public void parse_version_numbers()
        {
            PackageVersion pv = null;

            if (!PackageVersion.TryParse("1", out pv))
                Assert.Fail();
            if (pv.ToString() != "1")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2.3.4567890", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2.3.4567890")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2.3.4a", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2.3.4a")
                Assert.Fail();
        }

        [TestMethod]
        public void parse_suffix()
        {
            PackageVersion pv = null;

            if (!PackageVersion.TryParse("1.2_alpha", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_alpha")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2_alpha1", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_alpha1")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2_alpha12", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_alpha12")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2_beta", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_beta")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2_pre", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_pre")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2_rc", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_rc")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2_p", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_p")
                Assert.Fail();
        }

        [TestMethod]
        public void parse_revision_number()
        {
            PackageVersion pv = null;

            if (!PackageVersion.TryParse("1.2_alpha1-r1", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_alpha1-r1")
                Assert.Fail();

            if (!PackageVersion.TryParse("1.2_alpha1-r12345", out pv))
                Assert.Fail();
            if (pv.ToString() != "1.2_alpha1-r12345")
                Assert.Fail();
        }

        [TestMethod]
        public void compare_versions()
        {
            PackageVersion l = null;
            PackageVersion r = null;

            PackageVersion.TryParse("1.2_alpha1-r1", out l);
            PackageVersion.TryParse("1.2_alpha1-r1", out r);
            Assert.AreEqual(l, r);

            PackageVersion.TryParse("1.2_alpha1", out l);
            PackageVersion.TryParse("1.2_alpha1", out r);
            Assert.AreEqual(l, r);

            PackageVersion.TryParse("1.2_alpha", out l);
            PackageVersion.TryParse("1.2_alpha", out r);
            Assert.AreEqual(l, r);

            PackageVersion.TryParse("1.2", out l);
            PackageVersion.TryParse("1.2", out r);
            Assert.AreEqual(l, r);

            PackageVersion.TryParse("1.2_alpha1-r1", out l);
            PackageVersion.TryParse("1.2_alpha1-r2", out r);
            Assert.IsTrue(l < r);

            PackageVersion.TryParse("1.2_alpha1-r1", out l);
            PackageVersion.TryParse("1.2_alpha1", out r);
            Assert.IsTrue(l > r);

            PackageVersion.TryParse("1.2_alpha", out l);
            PackageVersion.TryParse("1.2_alpha1", out r);
            Assert.IsTrue(l < r);

            PackageVersion.TryParse("1.2_beta", out l);
            PackageVersion.TryParse("1.2_rc", out r);
            Assert.IsTrue(l < r);

            PackageVersion.TryParse("1.2", out l);
            PackageVersion.TryParse("1.2_p", out r);
            Assert.IsTrue(l < r);
        }
    }
}
