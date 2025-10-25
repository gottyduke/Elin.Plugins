## API 設定例

> [!Important]
> プロバイダーにニックネームを設定する際は、パラメータを編集する前に必ず「Reload（再読み込み）」をクリックしてください。
> JSON 形式に慣れていない場合は、[JSONLint](https://jsonlint.com/) のようなサイトで検証することをおすすめします。

Emmersive（Elin with AI）は、**function-calling（関数呼び出し）** または **tool-call（ツール呼び出し）**、さらに **structured
output（構造化出力）** または **json mode（JSON モード）** に対応した AI サービスを必要とします。

---

## 推論を有効にするかどうか

一部のモデルは「推論機能（reasoning）」を提供しており、より高品質な出力を生成できますが、その分、生成時間が長くなり、token
の消費も増えます。

Elin ゲームのコンテキストおよび Emmersive システムが数行の JSON 出力を主とする特性を考慮すると、推論機能を有効にすることによるコスト（遅延・token
消費・応答性の低下）はあまり得策ではないかもしれません。

すべての `reasoning_effort` / `thinkingBudget` はデフォルトで最低値に設定されていますが、必要に応じてパラメータで変更できます。

---

## Google Gemini（無料！）

[Google AI Studio](https://aistudio.google.com/projects) にアクセスし、プロジェクトを作成します。
無料枠を利用する場合は、**3 つのプロジェクト**を作成することを推奨します（レート制限は API キー単位ではなくプロジェクト単位で適用されます）。

**モデル**：`gemini-2.5-flash`
他のモデルに変更可能ですが、無料層では `gemini-2.5-pro` のような推論モデルはやや遅い場合があります。
デフォルトモデルは `gemini-2.5-flash` です。

**パラメータ参考**：[Google Gemini API Reference](https://ai.google.dev/api/generate-content#request-body)

**無料レート制限**：1 プロジェクトあたり 1 分間に 15 リクエスト、1 アカウントあたり 1 日 250 リクエスト。

**推奨 AI サービスのクールダウン時間**：`1`s

---

## NVIDIA NIM（無料！）

[NVIDIA Build](https://build.nvidia.com/settings/api-keys) にアクセスし、新しい API キーを作成します。

**ベース URL**：`https://integrate.api.nvidia.com/v1`

**モデル**：`deepseek-ai/deepseek-v3.1-terminus`
（または [NVIDIA モデル一覧](https://docs.api.nvidia.com/nim/reference/deepseek-ai-deepseek-v3_1-terminus) に掲載されている他のモデル）

**パラメータ：**

```json
{
  "response_format": {
    "type": "json_object"
  }
}
```

任意パラメータ：`temperature`、`top_p`、`max_tokens`、`frequency_penalty`、`presence_penalty`

**無料レート制限**：1 分間に 40 リクエスト。

**推奨クールダウン時間**：`1`s

---

## OpenAI ChatGPT

[OpenAI Platform](https://platform.openai.com/api-keys) にアクセスし、新しい API キーを生成します。

**ベース URL**：`https://api.openai.com/v1`

**モデル**：`gpt-5-nano`
（または [OpenAI モデル一覧](https://platform.openai.com/docs/pricing) にある任意のモデル）

**パラメータ**（Emmersive によりデフォルト提供）：

```json
{
  "frequency_penalty": 0.6,
  "reasoning_effort": "minimal",
  "response_format": {
    "type": "json_schema",
    "json_schema": {
      // 一部省略
    }
  }
}
```

参照：[OpenAI Chat Completion API Reference](https://platform.openai.com/docs/api-reference/chat/create)

---

## OpenAI 互換サービスプロバイダー

他のサービスプロバイダーを使用するのも簡単です。
ベース URL、モデル、パラメータを変更するだけで対応できます。

---

### DeepSeek

[DeepSeek Platform](https://platform.deepseek.com/api_keys) にアクセスし、新しい API キーを作成します。

**ベース URL**：`http://api.deepseek.com/v1`（またはシリコンフローなど他の提供元）

**モデル**：`deepseek-chat`（DeepSeekV3.2-Exp の非推論バージョン）

**パラメータ：**

```json
{
  "frequency_penalty": 0.6,
  "response_format": {
    "type": "json_object"
  }
}
```

参照：[DeepSeek Chat Completion API Reference](https://api-docs.deepseek.com/api/create-chat-completion)

---

### X.AI（grok）

[X.AI](https://docs.x.ai/docs/models) にアクセスし、ログインして新しい API キーを作成します。

**ベース URL**：`https://api.x.ai/v1`

**モデル**：`grok-4-fast-non-reasoning`
（または [X.AI モデル一覧](https://docs.x.ai/docs/models) にある任意のモデル）

**パラメータ：**

```json
{
  "response_format": {
    "type": "json_schema",
    "json_schema": {
        // 一部省略
    }
  }
}
```

参照：[X.AI Chat Completions API Reference](https://docs.x.ai/docs/api-reference#chat-completions)

---

### ローカル LLM（Ollama / WebUI）

設定方法は同じで、ベース URL をローカルポートに変更するだけです。

使用するモデルに応じて、モデル名とパラメータを調整してください。
