export const arrayIsNullOrEmpty = (array: unknown): boolean => {
	if (Array.isArray(array)) {
		return array.length === 0;
	} else {
		return true;
	}
};
