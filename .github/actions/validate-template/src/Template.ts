import { ErrorCode, JsonValue } from './types.ts';
import Result from './Result.ts';
import { arrayIsNullOrEmpty } from './utils.ts';

class Template {
	readonly #name: string;
	#manifest: JsonValue | null = null;
	#template: JsonValue | null = null;

	private get isEmpty(): boolean {
		return this.#template === null || this.#manifest === null;
	}

	constructor(name: string, path: string) {
		this.#name = name;
		const entries = Array.from(Deno.readDirSync(path));
		entries.forEach((entry) => {
			if (entry.name === 'manifest.json') {
				this.#manifest = JSON.parse(
					Deno.readTextFileSync(`${path}/${entry.name}`),
				);
			} else {
				this.#template = JSON.parse(
					Deno.readTextFileSync(`${path}/${entry.name}`),
				);
			}
		});
	}

	public validate(): Result {
		const result = new Result(this.#name);
		if (this.isEmpty) {
			// todo: add something
		} else {
			if (arrayIsNullOrEmpty(this.#manifest!.annotations)) {
				result.warnings.push({
					key: 'annotations',
					code: ErrorCode.ITEM_IS_NULL_OR_EMPTY,
				});
			}
			if (arrayIsNullOrEmpty(this.#manifest!.services)) {
				result.warnings.push({
					key: 'services',
					code: ErrorCode.ITEM_IS_NULL_OR_EMPTY,
				});
			}
			if (arrayIsNullOrEmpty(this.#manifest!.categories)) {
				result.warnings.push({
					key: 'categories',
					code: ErrorCode.ITEM_IS_NULL_OR_EMPTY,
				});
			}
		}
		return result;
	}
}

export default Template;
