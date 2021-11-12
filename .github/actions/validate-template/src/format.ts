import Result from './Result.ts';
import { ErrorCode, ItemIsNullOrEmpty, ValidateResult } from './types.ts';

const EOL = '\n';

const formatter = {
	[ErrorCode.ITEM_IS_NULL_OR_EMPTY]: (item: ItemIsNullOrEmpty): string =>
		`- "${item.key}" field is not specified. Do you want to add it?`,
};

const formatResult = (result: Result): string => {
	const details: string[] = [];
	details.push(
		`### Template **"${result.template}"** has ***${result.errors.length}*** ${
			result.errors.length > 1 ? 'errors' : 'error'
		} and ***${result.warnings.length}*** ${
			result.warnings.length > 1 ? 'warnings' : 'warning'
		}`,
	);
	if (result.hasError) {
		details.push('#### errors:');
		result.errors.forEach((error) =>
			details.push(formatter[error.code](error))
		);
		details.push(EOL);
	}
	if (result.hasWarning) {
		details.push('#### warnings:');
		result.warnings.forEach((warning) =>
			details.push(formatter[warning.code](warning))
		);
		details.push(EOL);
	}
	return details.join(EOL);
};

export const formatResults = (results: Result[]): ValidateResult => {
	let status: 0 | 1 = 0;
	const details: string[] = [];
	results.forEach((result) => {
		if (result.hasError || result.hasWarning) {
			status = 1;
			details.push(formatResult(result));
		}
	});
	return {
		status,
		detail: details.join(EOL),
	};
};
