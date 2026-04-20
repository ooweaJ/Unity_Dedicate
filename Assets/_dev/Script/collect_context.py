import os

def collect_files():
    print("--- 📄 코드 수집기 (Claude 전송용) ---")
    paths_input = input("수집할 파일 경로들을 입력하세요 (공백으로 구분): ")
    file_paths = paths_input.split()
    
    final_context = ""
    
    for path in file_paths:
        path = path.strip()
        if os.path.exists(path):
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
                # 클로드가 읽기 좋게 파일명과 코드를 마크다운으로 포맷팅
                final_context += f"\n\n### File: {path}\n```csharp\n{content}\n```"
            print(f"✅ 수집 완료: {path}")
        else:
            print(f"❌ 파일을 찾을 수 없음: {path}")

    # 결과물을 context_for_claude.txt로 저장
    with open("context_for_claude.txt", "w", encoding="utf-8") as out:
        out.write(final_context)
    
    print("\n--- ✨ 준비 완료! ---")
    print("'context_for_claude.txt' 파일이 생성되었습니다. 이 내용을 복사해서 클로드에 붙여넣으세요.")

if __name__ == "__main__":
    collect_files()