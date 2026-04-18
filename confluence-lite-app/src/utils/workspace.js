/**
 * 工作空间相关的工具函数
 */

/**
 * 空间颜色池（注意：顺序不能改变，否则已有空间的颜色会变化）
 */
const SPACE_COLORS = [
    "linear-gradient(135deg, #10b981, #059669)",  // green
    "linear-gradient(135deg, #3b82f6, #2563eb)",  // blue
    "linear-gradient(135deg, #8b5cf6, #7c3aed)",  // purple
    "linear-gradient(135deg, #f59e0b, #d97706)",  // orange
    "linear-gradient(135deg, #ef4444, #dc2626)",  // red
    "linear-gradient(135deg, #06b6d4, #0891b2)",  // cyan
];

/**
 * 根据空间 ID 生成一致的颜色
 * @param {number} id - 空间 ID
 * @returns {string} 渐变色
 */
export function getSpaceColorById(id) {
    if (!id) return SPACE_COLORS[0];
    return SPACE_COLORS[id % SPACE_COLORS.length];
}

/**
 * 获取空间首字母缩写
 * @param {Object} space - 空间对象
 * @returns {string} 首字母大写
 */
export function getSpaceInitial(space) {
    if (!space) return "?";
    const key = space.key || space.name || "";
    return key.charAt(0).toUpperCase();
}
