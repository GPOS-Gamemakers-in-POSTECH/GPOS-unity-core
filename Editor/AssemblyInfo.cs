using System.Runtime.CompilerServices;

// 에디터 툴의 순수 로직(경로 검증, 파일 이름 정리 등)을 public API 로 열지 않고 테스트하기 위해
// 테스트 어셈블리에만 internal 접근을 허용합니다.
[assembly: InternalsVisibleTo("GPOS.Core.Tests")]
