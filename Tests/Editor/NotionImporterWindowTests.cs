using GPOS.Core.Editor;
using NUnit.Framework;

namespace GPOS.Core.Tests
{
    public class NotionImporterWindowTests
    {
        // --- TryResolveOutputFolder -------------------------------------------------

        [Test]
        public void Assets_아래_경로는_통과한다()
        {
            Assert.IsTrue(NotionImporterWindow.TryResolveOutputFolder("Assets/GPOS/Notion", out string folder, out string error));
            Assert.AreEqual("Assets/GPOS/Notion", folder);
            Assert.IsNull(error);
        }

        [Test]
        public void Assets_단독도_통과한다()
        {
            Assert.IsTrue(NotionImporterWindow.TryResolveOutputFolder("Assets", out string folder, out _));
            Assert.AreEqual("Assets", folder);
        }

        [Test]
        public void 앞뒤_공백과_끝_슬래시는_정리된다()
        {
            Assert.IsTrue(NotionImporterWindow.TryResolveOutputFolder("  Assets/GPOS/  ", out string folder, out _));
            Assert.AreEqual("Assets/GPOS", folder);
        }

        [Test]
        public void 역슬래시는_슬래시로_바뀐다()
        {
            Assert.IsTrue(NotionImporterWindow.TryResolveOutputFolder(@"Assets\GPOS\Notion", out string folder, out _));
            Assert.AreEqual("Assets/GPOS/Notion", folder);
        }

        // params object[] 오버로드 때문에 맨 null 은 배열 자체로 해석됩니다. 반드시 캐스팅해야 합니다.
        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("   ")]
        public void 빈_폴더는_거부한다(string input)
        {
            Assert.IsFalse(NotionImporterWindow.TryResolveOutputFolder(input, out string folder, out string error));
            Assert.IsNull(folder);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void 슬래시_하나는_거부한다()
        {
            // TrimEnd('/') 후 빈 문자열이 되어 드라이브 루트에 파일을 쓰던 케이스.
            Assert.IsFalse(NotionImporterWindow.TryResolveOutputFolder("/", out string folder, out _));
            Assert.IsNull(folder);
        }

        [TestCase("C:/Temp")]
        [TestCase("Library/Cache")]
        [TestCase("Packages/com.gpos.core")]
        [TestCase("AssetsFake/Notion")]
        public void Assets_밖_경로는_거부한다(string input)
        {
            Assert.IsFalse(NotionImporterWindow.TryResolveOutputFolder(input, out string folder, out string error));
            Assert.IsNull(folder);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void 상위_폴더_이동은_거부한다()
        {
            Assert.IsFalse(NotionImporterWindow.TryResolveOutputFolder("Assets/../Library", out string folder, out _));
            Assert.IsNull(folder);
        }

        [Test]
        public void 경로에_쓸_수_없는_문자는_거부한다()
        {
            Assert.IsFalse(NotionImporterWindow.TryResolveOutputFolder("Assets/GPOS?Notion", out string folder, out _));
            Assert.IsNull(folder);
        }

        // --- Sanitize ---------------------------------------------------------------

        [Test]
        public void 정상_이름은_그대로_둔다()
        {
            Assert.AreEqual("Quest DB", NotionImporterWindow.Sanitize("Quest DB"));
        }

        [TestCase("a/b", "a_b")]
        [TestCase(@"a\b", "a_b")]
        [TestCase("a:b", "a_b")]
        [TestCase("a*b?c", "a_b_c")]
        [TestCase("a<b>c|d", "a_b_c_d")]
        public void 쓸_수_없는_문자는_밑줄로_바꾼다(string input, string expected)
        {
            // 플랫폼과 무관하게 같은 결과가 나와야 합니다. (유닉스의 Path.GetInvalidFileNameChars 는 '/' 와 '\0' 뿐)
            Assert.AreEqual(expected, NotionImporterWindow.Sanitize(input));
        }

        [Test]
        public void 앞뒤_공백은_제거한다()
        {
            Assert.AreEqual("Quest", NotionImporterWindow.Sanitize("  Quest  "));
        }

        [Test]
        public void 끝의_마침표는_제거한다()
        {
            // 윈도우는 '.' 로 끝나는 파일 이름을 만들 수 없습니다.
            Assert.AreEqual("Quest", NotionImporterWindow.Sanitize("Quest."));
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("..")]
        [TestCase(".")]
        public void 남는_글자가_없으면_빈_문자열을_돌려준다(string input)
        {
            // 호출부는 이 결과를 보고 해당 항목을 건너뜁니다. ('.json' 같은 파일이 생기지 않도록)
            Assert.AreEqual(string.Empty, NotionImporterWindow.Sanitize(input));
        }

        // --- ExtractDatabaseId -------------------------------------------------------

        private const string Id = "a8aec43384f447ed84390e8e42c2e089";

        [Test]
        public void ID_만_넣으면_그대로_돌려준다()
        {
            Assert.AreEqual(Id, NotionImporterWindow.ExtractDatabaseId(Id));
        }

        [Test]
        public void 대시가_들어간_UUID_도_받는다()
        {
            Assert.AreEqual(Id, NotionImporterWindow.ExtractDatabaseId("a8aec433-84f4-47ed-8439-0e8e42c2e089"));
        }

        [Test]
        public void URL_의_쿼리를_잘라낸다()
        {
            Assert.AreEqual(Id, NotionImporterWindow.ExtractDatabaseId(
                "https://www.notion.so/" + Id + "?v=b5f8c5b7f4a44a1e9e0b9f8c5b7f4a44&pvs=4"));
        }

        [Test]
        public void 제목_슬러그가_붙어_있어도_ID_만_뽑는다()
        {
            // 슬러그에도 '-' 가 있으므로 단순히 대시만 지우면 제목이 앞에 붙습니다.
            Assert.AreEqual(Id, NotionImporterWindow.ExtractDatabaseId(
                "https://www.notion.so/myteam/Quest-Table-" + Id + "?v=abc"));
        }

        [Test]
        public void 제목_슬러그와_대시_UUID_가_함께_있어도_ID_만_뽑는다()
        {
            Assert.AreEqual(Id, NotionImporterWindow.ExtractDatabaseId(
                "https://www.notion.so/myteam/Quest-Table-a8aec433-84f4-47ed-8439-0e8e42c2e089?v=abc"));
        }

        [Test]
        public void 프래그먼트와_끝_슬래시를_처리한다()
        {
            Assert.AreEqual(Id, NotionImporterWindow.ExtractDatabaseId(
                "https://www.notion.so/" + Id + "/#block"));
        }

        [Test]
        public void 앞뒤_공백을_허용한다()
        {
            Assert.AreEqual(Id, NotionImporterWindow.ExtractDatabaseId("  " + Id + "  "));
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("https://www.notion.so/myworkspace")]
        [TestCase("Quest-Table")]
        [TestCase("a8aec43384f447ed84390e8e42c2e08")]
        public void 뽑아낼_수_없으면_null_을_돌려준다(string input)
        {
            // 호출부는 null 을 받으면 사용자가 입력한 값을 그대로 남겨둡니다.
            Assert.IsNull(NotionImporterWindow.ExtractDatabaseId(input));
        }

        [Test]
        public void hex_가_아닌_32자는_거부한다()
        {
            Assert.IsNull(NotionImporterWindow.ExtractDatabaseId("z8aec43384f447ed84390e8e42c2e089"));
        }
    }
}
