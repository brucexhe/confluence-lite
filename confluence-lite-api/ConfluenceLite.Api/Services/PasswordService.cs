using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 密码哈希服务 - Native AOT 兼容
/// </summary>
public static class PasswordService
{
    private const int SaltSize = 128 / 8; // 128 bit
    private const int IterationCount = 10000;
    private const int NumBytesRequested = 256 / 8; // 256 bit
    private const KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA256;

    /// <summary>
    /// 哈希密码
    /// </summary>
    /// <param name="password">原始密码</param>
    /// <returns>密码哈希值</returns>
    public static string HashPassword(string password)
    {
        // 生成随机盐值
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // 使用PBKDF2生成哈希
        var hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: Prf,
            iterationCount: IterationCount,
            numBytesRequested: NumBytesRequested
        );

        // 组合盐值和哈希值
        var hashBytes = new byte[SaltSize + NumBytesRequested];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, NumBytesRequested);

        // 转换为Base64字符串
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">原始密码</param>
    /// <param name="hashedPassword">哈希后的密码</param>
    /// <returns>是否匹配</returns>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            // 将Base64字符串转换为字节数组
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // 验证长度
            if (hashBytes.Length != SaltSize + NumBytesRequested)
            {
                return false;
            }

            // 提取盐值
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // 使用相同的盐值计算哈希
            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: Prf,
                iterationCount: IterationCount,
                numBytesRequested: NumBytesRequested
            );

            // 比较哈希值
            for (int i = 0; i < NumBytesRequested; i++)
            {
                if (hashBytes[SaltSize + i] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
