export interface JsonValue {
	[propName: string]:
		| string
		| number
		| boolean
		| (string | number | boolean)[]
		| JsonValue;
}

export type ValidateResult = {
	status: 0 | 1;
	detail: string;
};

export enum ErrorCode {
	ITEM_IS_NULL_OR_EMPTY,
	PARSE_JSON_FAIL,
}

export type ItemIsNullOrEmpty = {
	code: ErrorCode.ITEM_IS_NULL_OR_EMPTY;
	key: string;
};

export type ParseJSONFail = {
	code: ErrorCode.PARSE_JSON_FAIL;
	detail: string;
};

export type ResultItem = ItemIsNullOrEmpty | ParseJSONFail;
