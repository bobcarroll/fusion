using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Fusion.Framework;

namespace tests
{
    [TestClass]
    public class AtomTests
    {
        [TestMethod]
        public void parse_full_atom_with_slot()
        {
            Atom a = Atom.Parse("=foo/bar-1.2:5", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "=");
            Assert.AreEqual(a.CategoryPart, "foo");
            Assert.AreEqual(a.PackagePart, "bar");
            Assert.AreEqual(a.PackageName, "foo/bar");
            Assert.AreEqual(a.Version.ToString(), "1.2");
            Assert.AreEqual(a.Slot, (uint)5);
        }

        [TestMethod]
        public void parse_full_atom_without_slot()
        {
            Atom a = Atom.Parse("=foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "=");
            Assert.AreEqual(a.CategoryPart, "foo");
            Assert.AreEqual(a.PackagePart, "bar");
            Assert.AreEqual(a.PackageName, "foo/bar");
            Assert.AreEqual(a.Version.ToString(), "1.2");
            Assert.AreEqual(a.Slot, (uint)0);
        }

        [TestMethod]
        public void parse_atom_implicit_equals()
        {
            Atom a = Atom.Parse("foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "=");
        }

        [TestMethod]
        public void parse_atom_full_name_without_version()
        {
            Atom a = Atom.Parse("foo/bar", AtomParseOptions.WithoutVersion);
            Assert.AreEqual(a.PackageName, "foo/bar");
        }

        [TestMethod]
        public void parse_atom_full_name_without_version_demand_with_version()
        {
            try {
                Atom.Parse("foo/bar", AtomParseOptions.VersionRequired);
            } catch (BadAtomException) {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void parse_atom_short_name_without_version()
        {
            Atom a = Atom.Parse("bar", AtomParseOptions.WithoutVersion);
            Assert.AreEqual(a.PackageName, "bar");
        }

        [TestMethod]
        public void parse_atom_short_name_without_version_demand_with_version()
        {
            try {
                Atom.Parse("bar", AtomParseOptions.VersionRequired);
            } catch (BadAtomException) {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void parse_package_shortname_with_version()
        {
            Atom a = Atom.Parse("bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "=");
            Assert.AreEqual(a.PackageName, "bar");
            Assert.AreEqual(a.Version.ToString(), "1.2");
        }

        [TestMethod]
        public void parse_comparison_operator_with_version()
        {
            Atom a = Atom.Parse("=foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "=");

            a = Atom.Parse(">foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, ">");

            a = Atom.Parse(">=foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, ">=");

            a = Atom.Parse("<foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "<");

            a = Atom.Parse("<=foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "<=");

            a = Atom.Parse("!foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.Comparison, "!");
        }

        [TestMethod]
        public void parse_atom_with_version_deman_without_version()
        {
            try {
                Atom.Parse("=foo/bar-1.2", AtomParseOptions.WithoutVersion);
            } catch (BadAtomException) {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void parse_atom_with_operator_demand_without_version()
        {
            try {
                Atom.Parse("=foo/bar", AtomParseOptions.WithoutVersion);
            } catch (BadAtomException) {
                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void atom_to_string()
        {
            Atom a = Atom.Parse("foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.ToString(), "foo/bar-1.2");

            a = Atom.Parse("foo/bar-1.2:5", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.ToString(), "foo/bar-1.2:5");

            a = Atom.Parse(">foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.AreEqual(a.ToString(), ">foo/bar-1.2");
        }

        [TestMethod]
        public void atom_match()
        {
            Atom l = Atom.Parse("foo/bar-1.2", AtomParseOptions.VersionRequired);
            Atom r = Atom.Parse("foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.IsTrue(r.Match(l));

            l = Atom.Parse("foo/bar-1.3", AtomParseOptions.VersionRequired);
            r = Atom.Parse(">foo/bar-1.2", AtomParseOptions.VersionRequired);
            Assert.IsTrue(r.Match(l));

            l = Atom.Parse("foo/bar-1.2", AtomParseOptions.VersionRequired);
            r = Atom.Parse("<foo/bar-1.3", AtomParseOptions.VersionRequired);
            Assert.IsTrue(r.Match(l));
        }

        [TestMethod]
        public void atom_match_operator_left_side()
        {
            Atom l = Atom.Parse("<foo/bar-1.2", AtomParseOptions.VersionRequired);
            Atom r = Atom.Parse("foo/bar-1.3", AtomParseOptions.VersionRequired);
            Assert.IsFalse(r.Match(l));

            l = Atom.Parse("<foo/bar-1.3", AtomParseOptions.VersionRequired);
            r = Atom.Parse("foo/bar-1.3", AtomParseOptions.VersionRequired);
            Assert.IsTrue(r.Match(l));
        }
    }
}
